using Stellar.Core.Quantization;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Domains;

/// <summary>
/// The base of the domains: the checks every domain makes, before the work of the driver.
/// </summary>
/// <remarks>
/// <para>Each public member checks that the domain is not disposed, that it has the capability, and brings the
/// path to its one form (free for a path that is in it already), then calls its <c>Core</c> method. A driver
/// overrides the <c>Core</c> methods of what it can do and declares it in the capabilities.</para>
/// </remarks>
public abstract class DomainBase
    : Quant<MetaQuant>, IDomain
{
    private static readonly IReadOnlyDictionary<string, string> NoOptions = new Dictionary<string, string>();
    private volatile bool _disposed;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public DomainScheme Scheme { get; }

    /// <inheritdoc/>
    public string Source { get; }

    /// <inheritdoc/>
    public DomainCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the settings of the driver this domain was mounted with.
    /// </summary>
    protected IReadOnlyDictionary<string, string> Options { get; }

    /// <summary>
    /// Gets the file system the domain is mounted in.
    /// </summary>
    protected IFileSystem FileSystem { get; }

    /// <summary>
    /// Gets whether the domain is disposed.
    /// </summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Makes the domain.
    /// </summary>
    /// <param name="mount">What was mounted.</param>
    /// <param name="fileSystem">The file system it is mounted in.</param>
    /// <param name="capabilities">What the domain can do.</param>
    protected DomainBase(DomainMount mount, IFileSystem fileSystem, DomainCapabilities capabilities)
        : base(new MetaQuant())
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        Name = FileSystemNames.NormalizeName(mount.Name, nameof(mount));
        Scheme = mount.Scheme;
        Source = mount.Source ?? string.Empty;
        Options = mount.Options ?? NoOptions;
        FileSystem = fileSystem;
        Capabilities = capabilities;
    }

    /// <summary>
    /// Makes the exception for what the domain cannot do.
    /// </summary>
    protected NotSupportedException Unsupported(string what) =>
        new($"The domain '{Name}' ({Scheme}) cannot {what}");

    /// <summary>
    /// Makes the exception for a file that is not there.
    /// </summary>
    protected FileNotFoundException NotFound(string path) =>
        new($"No file '{PathOf(path)}'", PathOf(path).ToString());

    /// <summary>
    /// Makes the exception for a directory that is not there.
    /// </summary>
    protected DirectoryNotFoundException DirectoryNotFound(string path) =>
        new($"No directory '{PathOf(path)}'");

    /// <summary>
    /// Throws if the domain is disposed.
    /// </summary>
    protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private void Require(DomainCapabilities capability, string what)
    {
        if ((Capabilities & capability) != capability) throw Unsupported(what);
    }

    private void RequireFor(FileOpenOptions options)
    {
        if (options.Writes) Require(DomainCapabilities.Write, "write");
        else Require(DomainCapabilities.Read, "read");

        if ((options.Flags & FileOpenFlags.Atomic) == 0) return;

        Require(DomainCapabilities.AtomicWrite, "replace files atomically");
        if (options.Mode is not (FileMode.Create or FileMode.CreateNew) || options.Access != FileAccess.Write)
            throw new ArgumentException(
                "An atomic write creates the file anew: FileMode.Create or CreateNew, FileAccess.Write",
                nameof(options));
    }

    /// <summary>Whether a file or a directory exists.</summary>
    protected abstract bool ExistsCore(string path);

    /// <summary>What is known of a file or a directory.</summary>
    protected abstract bool TryGetEntryCore(string path, out FileEntry entry);

    /// <summary>The entries of a directory.</summary>
    protected virtual IEnumerable<FileEntry> EnumerateCore(string directory, string pattern, bool recursive) =>
        throw Unsupported("list files");

    /// <summary>Opens a file; the options are checked against the capabilities.</summary>
    protected abstract Stream OpenCore(string path, FileOpenOptions options);

    /// <summary>Opens a file without blocking; by default, <see cref="OpenCore"/>.</summary>
    protected virtual ValueTask<Stream> OpenAsyncCore(string path, FileOpenOptions options,
        CancellationToken cancellationToken) =>
        new(OpenCore(path, options));

    /// <summary>Opens a file for reads at offsets.</summary>
    protected virtual IRandomAccessFile OpenRandomAccessCore(string path) => throw Unsupported("read at offsets");

    /// <summary>The bytes of a file in memory.</summary>
    protected virtual bool TryGetMemoryCore(string path, out ReadOnlyMemory<byte> data)
    {
        data = default;
        return false;
    }

    /// <summary>Creates a directory.</summary>
    protected virtual void CreateDirectoryCore(string path) => throw Unsupported("create directories");

    /// <summary>Deletes a file or a directory, never the root.</summary>
    protected virtual bool DeleteCore(string path, bool recursive) => throw Unsupported("delete");

    /// <summary>Moves a file or a directory; neither is the root, and the destination is not inside the source.</summary>
    protected virtual void MoveCore(string source, string destination, bool overwrite) => throw Unsupported("move");

    /// <summary>Watches a directory.</summary>
    protected virtual IDisposable WatchCore(string directory, bool recursive, Action<FileChange> handler) =>
        throw Unsupported("watch");

    /// <summary>
    /// Releases what the domain holds: called once.
    /// </summary>
    protected virtual void DisposeCore()
    {
    }

    /// <summary>
    /// Reads a yes-or-no setting of the mount.
    /// </summary>
    /// <param name="options">The settings.</param>
    /// <param name="key">The name of the setting.</param>
    /// <param name="fallback">The value when the setting is absent.</param>
    /// <returns>The value.</returns>
    protected static bool GetFlag(IReadOnlyDictionary<string, string>? options, string key, bool fallback)
    {
        if (options is null
            || !options.TryGetValue(key, out var text))
            return fallback;
        return bool.TryParse(text, out var value)
            ? value
            : throw new ArgumentException($"The setting '{key}' is '{text}': expected true or false");
    }

    /// <summary>
    /// Gets the full path of a path of this domain.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <returns>The full path.</returns>
    protected FilePath PathOf(string path) => FilePath.Create(Name, path);

    /// <summary>
    /// Makes the entry of a file, its content type from the file system.
    /// </summary>
    protected FileEntry FileEntryOf(string path, long length,
        DateTime? creationTimeUtc, DateTime? lastWriteTimeUtc,
        long revision)
    {
        var full = PathOf(path);
        return new FileEntry(full, FileEntryKind.File, length,
            creationTimeUtc, lastWriteTimeUtc,
            FileSystem.ContentTypes.Resolve(full), revision);
    }

    /// <summary>
    /// Makes the entry of a directory.
    /// </summary>
    protected FileEntry DirectoryEntryOf(string path,
        DateTime? creationTimeUtc = null, DateTime? lastWriteTimeUtc = null)
    {
        var full = PathOf(path);
        return new FileEntry(full, FileEntryKind.Directory, 0,
            creationTimeUtc, lastWriteTimeUtc,
            default, 0);
    }

    /// <inheritdoc/>
    public bool Exists(string path)
    {
        ThrowIfDisposed();
        return ExistsCore(FileSystemNames.NormalizePath(path));
    }

    /// <inheritdoc/>
    public bool TryGetEntry(string path, out FileEntry entry)
    {
        ThrowIfDisposed();
        return TryGetEntryCore(FileSystemNames.NormalizePath(path), out entry);
    }

    /// <inheritdoc/>
    public IEnumerable<FileEntry> Enumerate(string directory, string pattern = "*", bool recursive = false)
    {
        ThrowIfDisposed();
        Require(DomainCapabilities.Enumerate, "list files");
        ArgumentNullException.ThrowIfNull(pattern);
        return EnumerateCore(FileSystemNames.NormalizePath(directory, nameof(directory)), pattern, recursive);
    }

    /// <inheritdoc/>
    public Stream Open(string path, FileOpenOptions options)
    {
        ThrowIfDisposed();
        RequireFor(options);
        return OpenCore(FileSystemNames.NormalizePath(path), options);
    }

    /// <inheritdoc/>
    public ValueTask<Stream> OpenAsync(string path, FileOpenOptions options,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        RequireFor(options);
        return OpenAsyncCore(FileSystemNames.NormalizePath(path), options, cancellationToken);
    }

    /// <inheritdoc/>
    public IRandomAccessFile OpenRandomAccess(string path)
    {
        ThrowIfDisposed();
        Require(DomainCapabilities.RandomAccess, "read at offsets");
        return OpenRandomAccessCore(FileSystemNames.NormalizePath(path));
    }

    /// <inheritdoc/>
    public bool TryGetMemory(string path, out ReadOnlyMemory<byte> data)
    {
        ThrowIfDisposed();
        data = default;
        return (Capabilities & DomainCapabilities.Memory) != 0
               && TryGetMemoryCore(FileSystemNames.NormalizePath(path), out data);
    }

    /// <inheritdoc/>
    public void CreateDirectory(string path)
    {
        ThrowIfDisposed();
        Require(DomainCapabilities.Write, "create directories");
        CreateDirectoryCore(FileSystemNames.NormalizePath(path));
    }

    /// <inheritdoc/>
    public bool Delete(string path, bool recursive = false)
    {
        ThrowIfDisposed();
        Require(DomainCapabilities.Write, "delete");
        var normalized = FileSystemNames.NormalizePath(path);
        if (normalized.Length == 0) throw new IOException($"The root of the domain '{Name}' cannot be deleted");
        return DeleteCore(normalized, recursive);
    }

    /// <inheritdoc/>
    public void Move(string source, string destination, bool overwrite = false)
    {
        ThrowIfDisposed();
        Require(DomainCapabilities.Write, "move");
        var from = FileSystemNames.NormalizePath(source, nameof(source));
        var to = FileSystemNames.NormalizePath(destination, nameof(destination));
        if (from.Length == 0 || to.Length == 0)
            throw new IOException($"The root of the domain '{Name}' cannot be moved");
        if (from == to) return;
        if (to.StartsWith(from + "/", StringComparison.Ordinal))
            throw new IOException($"'{PathOf(from)}' cannot move into itself");
        MoveCore(from, to, overwrite);
    }

    /// <inheritdoc/>
    public IDisposable Watch(string directory, bool recursive, Action<FileChange> handler)
    {
        ThrowIfDisposed();
        Require(DomainCapabilities.Watch, "watch");
        ArgumentNullException.ThrowIfNull(handler);
        return WatchCore(FileSystemNames.NormalizePath(directory, nameof(directory)), recursive, handler);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Name}:// ({Scheme} {Source})";
}
