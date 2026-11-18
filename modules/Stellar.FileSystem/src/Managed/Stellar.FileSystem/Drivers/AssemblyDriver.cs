using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using Stellar.Core.Quantization;
using Stellar.FileSystem.Domains;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Drivers;

/// <summary>
/// Mounts the resources embedded in an assembly (<c>asm</c>), read-only. The source is the name of the assembly.
/// </summary>
/// <remarks>
/// <para>A resource is a file at its logical name. A name with '/' or '\' is a path, so a project embeds a tree with
/// <c>&lt;EmbeddedResource Include="Content\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)"/&gt;</c>; any
/// other name is a file at the root, dots and all.</para>
/// <para>The option <c>prefix</c> takes only the resources whose names start with it, and cuts it off.</para>
/// <para>The bytes are read where the assembly lies in memory: <see cref="IDomain.TryGetMemory"/> gives them with no
/// copy.</para>
/// </remarks>
public sealed class AssemblyDriver()
    : Quant<MetaQuant>(new MetaQuant()), IFileDriver
{
    /// <inheritdoc/>
    public DomainScheme Scheme => DomainScheme.Assembly;

    /// <inheritdoc/>
    public IDomain Mount(DomainMount mount, IFileSystem fileSystem) => new AssemblyDomain(mount, fileSystem);
}

internal sealed class AssemblyDomain
    : DomainBase
{
    private readonly Assembly _assembly;

    private readonly Dictionary<string, string> _resources = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal) { string.Empty };
    private readonly ConcurrentDictionary<string, ReadOnlyMemory<byte>> _memory = new(StringComparer.Ordinal);
    private readonly long _revision;
    private readonly DateTime? _lastWriteTimeUtc;

    private static Assembly Load(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("The source of an asm domain is the name of an assembly");

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (string.Equals(assembly.GetName().Name, source, StringComparison.OrdinalIgnoreCase))
                return assembly;

        return Assembly.Load(new AssemblyName(source));
    }

    public AssemblyDomain(DomainMount mount, IFileSystem fileSystem)
        : base(mount, fileSystem,
            DomainCapabilities.Read
            | DomainCapabilities.Enumerate
            | DomainCapabilities.RandomAccess
            | DomainCapabilities.Memory)
    {
        _assembly = Load(mount.Source);
        string? prefix = null;
        mount.Options?.TryGetValue("prefix", out prefix);

        foreach (var name in _assembly.GetManifestResourceNames())
        {
            var logical = name;
            if (prefix is not null)
            {
                if (!name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                logical = name.Substring(prefix.Length);
            }

            // A resource whose name cannot be a path is not a file of the domain
            if (!FileSystemNames.TryNormalizePath(logical, out var path) || path.Length == 0) continue;
            if (!_resources.TryAdd(path, name)) continue;
            foreach (var ancestor in DomainPaths.AncestorsOf(path))
                _directories.Add(ancestor);
        }

        // The same build of the assembly, the same bytes
        _revision = BitConverter.ToInt64(_assembly.ManifestModule.ModuleVersionId.ToByteArray(), 0);
        _lastWriteTimeUtc = _assembly.Location.Length > 0 ? File.GetLastWriteTimeUtc(_assembly.Location) : null;
    }

    private Stream OpenResource(string path)
    {
        if (!_resources.TryGetValue(path, out var name)) throw NotFound(path);
        return _assembly.GetManifestResourceStream(name) ?? throw NotFound(path);
    }

    private long LengthOf(string path)
    {
        using var stream = OpenResource(path);
        return stream.CanSeek ? stream.Length : -1;
    }

    private sealed unsafe class UnmanagedMemory(byte* pointer, int length)
        : MemoryManager<byte>
    {
        public override Span<byte> GetSpan() => new(pointer, length);

        public override MemoryHandle Pin(int elementIndex = 0) => new(pointer + elementIndex);

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }

    protected override bool ExistsCore(string path) =>
        _resources.ContainsKey(path) || _directories.Contains(path);

    protected override bool TryGetEntryCore(string path, out FileEntry entry)
    {
        if (_resources.ContainsKey(path))
        {
            entry = FileEntryOf(path, LengthOf(path), _lastWriteTimeUtc, _lastWriteTimeUtc, _revision);
            return true;
        }

        entry = _directories.Contains(path)
            ? DirectoryEntryOf(path, _lastWriteTimeUtc, _lastWriteTimeUtc)
            : default;

        return !entry.Path.IsEmpty;
    }

    protected override IEnumerable<FileEntry> EnumerateCore(string directory, string pattern, bool recursive)
    {
        if (!_directories.Contains(directory))
            throw DirectoryNotFound(directory);

        var entries = new List<FileEntry>();
        foreach (var path in _directories)
            if (DomainPaths.IsIn(path, directory, recursive) && DomainPaths.Matches(pattern, DomainPaths.NameOf(path)))
                entries.Add(DirectoryEntryOf(path, _lastWriteTimeUtc, _lastWriteTimeUtc));

        foreach (var path in _resources.Keys)
            if (DomainPaths.IsIn(path, directory, recursive) && DomainPaths.Matches(pattern, DomainPaths.NameOf(path)))
                entries.Add(FileEntryOf(path, LengthOf(path), _lastWriteTimeUtc, _lastWriteTimeUtc, _revision));

        entries.Sort((left, right) => left.Path.CompareTo(right.Path));
        return entries;
    }

    protected override Stream OpenCore(string path, FileOpenOptions options)
    {
        if (options.Mode is not (FileMode.Open or FileMode.OpenOrCreate))
            throw Unsupported($"open with {options.Mode}");
        return OpenResource(path);
    }

    protected override bool TryGetMemoryCore(string path, out ReadOnlyMemory<byte> data)
    {
        if (_memory.TryGetValue(path, out data)) return true;
        if (!_resources.ContainsKey(path)) return false;

        using var stream = OpenResource(path);
        if (stream is not UnmanagedMemoryStream unmanaged) return false;

        // The resource lies in the image of the assembly, which lives as long as the domain holds it
        unsafe
        {
            data = new UnmanagedMemory(unmanaged.PositionPointer, checked((int)unmanaged.Length)).Memory;
        }

        data = _memory.GetOrAdd(path, data);
        return true;
    }

    protected override IRandomAccessFile OpenRandomAccessCore(string path)
    {
        if (TryGetMemoryCore(path, out var data)) return new MemoryRandomAccessFile(PathOf(path), data);

        // A dynamic assembly: its resources are streams only
        using var stream = OpenResource(path);
        var copy = new MemoryStream();
        stream.CopyTo(copy);
        return new MemoryRandomAccessFile(PathOf(path), copy.ToArray());
    }
}
