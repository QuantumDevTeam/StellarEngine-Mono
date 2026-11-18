using Stellar.Core.Data.Catalog;
using Stellar.Core.Quantization;
using Stellar.FileSystem.Drivers;
using Stellar.Kernel.Data.Catalog;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem;

/// <summary>
/// The file system of one engine instance: its drivers, the domains they mounted and the content types.
/// </summary>
/// <remarks>
/// Nothing of it is static: make one per engine instance. <see cref="CreateDefault"/> gives one with the drivers of
/// this module (<c>dir</c>, <c>asm</c>, <c>mem</c>, <c>overlay</c>) and the common content types; mounting the
/// domains is up to the runtime.
/// </remarks>
public sealed class VirtualFileSystem
    : Quant<MetaQuant>, IFileSystem
{
    // A driver is set under its own scheme
    private sealed class DriverCatalog
        : Catalog<DomainScheme, IFileDriver>
    {
        protected override DomainScheme NormalizeKey(DomainScheme key)
        {
            return string.IsNullOrEmpty(key.Name)
                ? throw new ArgumentException("A driver needs a scheme", nameof(key))
                : key;
        }

        protected override void Validate(DomainScheme key, IFileDriver value)
        {
            base.Validate(key, value);
            if (value.Scheme != key)
                throw new ArgumentException($"The driver of '{value.Scheme}' cannot be set under '{key}'",
                    nameof(value));
        }
    }

    // Names in any case; a missing domain says so
    private sealed class DomainCatalog
        : Catalog<string, IDomain>
    {
        protected override string NormalizeKey(string key)
        {
            return FileSystemNames.NormalizeName(key, nameof(key));
        }

        protected override Exception NotFound(string key)
        {
            return new DomainNotFoundException(key);
        }
    }

    private readonly DriverCatalog _drivers = new();
    private readonly DomainCatalog _domains = new();
    private readonly Lock _mounting = new();
    private volatile bool _disposed;

    /// <inheritdoc/>
    public IContentTypeCatalog ContentTypes { get; }

    /// <summary>
    /// Makes a file system with no drivers and no domains.
    /// </summary>
    /// <param name="contentTypes">The content types; an empty catalog if <c>null</c>.</param>
    public VirtualFileSystem(IContentTypeCatalog? contentTypes = null)
        : base(new MetaQuant())
    {
        ContentTypes = contentTypes ?? new ContentTypeCatalog();
    }

    /// <summary>
    /// Makes a file system with the drivers of this module and the common content types.
    /// </summary>
    /// <returns>The file system, with no domain mounted.</returns>
    public static VirtualFileSystem CreateDefault()
    {
        var fileSystem = new VirtualFileSystem(ContentTypeCatalog.CreateDefault());
        fileSystem.AddDriver(new DirectoryDriver());
        fileSystem.AddDriver(new AssemblyDriver());
        fileSystem.AddDriver(new MemoryDriver());
        fileSystem.AddDriver(new OverlayDriver());
        return fileSystem;
    }

    /// <inheritdoc/>
    public ICatalog<DomainScheme, IFileDriver> Drivers => _drivers;

    /// <inheritdoc/>
    // ReSharper disable once InconsistentlySynchronizedField
    public IReadOnlyCatalog<string, IDomain> Domains => _domains;

    /// <inheritdoc/>
    public event Action<IDomain>? Mounted;

    /// <inheritdoc/>
    public event Action<IDomain>? Unmounting;

    /// <inheritdoc/>
    public IDomain Mount(DomainMount mount)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var name = FileSystemNames.NormalizeName(mount.Name, nameof(mount));
        if (!_drivers.TryGet(mount.Scheme, out var driver))
            throw new InvalidOperationException($"No driver of the scheme '{mount.Scheme}' is there, " +
                                                $"for the domain '{name}'");

        IDomain domain;
        lock (_mounting)
        {
            if (_domains.Contains(name))
                throw new InvalidOperationException($"A domain '{name}' is mounted already");

            domain = driver.Mount(mount with { Name = name }, this);
            if (domain.Name != name)
            {
                domain.Dispose();
                throw new InvalidOperationException($"The driver of '{mount.Scheme}' " +
                                                    $"mounted '{domain.Name}' instead of '{name}'");
            }

            _domains.Set(name, domain);
        }

        Mounted?.Invoke(domain);
        return domain;
    }

    /// <inheritdoc/>
    public bool Unmount(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        IDomain? domain;
        lock (_mounting)
        {
            if (!_domains.TryRemove(name, out domain)) return false;
        }

        // Out of the file system, still working: whoever holds data of it lets go
        try
        {
            Unmounting?.Invoke(domain);
        }
        finally
        {
            domain.Dispose();
        }

        return true;
    }

    /// <summary>
    /// Unmounts every domain.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var name in _domains.Keys) Unmount(name);
    }
}
