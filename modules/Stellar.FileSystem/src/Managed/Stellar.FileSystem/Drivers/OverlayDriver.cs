using Stellar.Core.Quantization;
using Stellar.FileSystem.Domains;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Drivers;

/// <summary>
/// Mounts several domains as layers of one (<c>overlay</c>): the source names them, first wins, such as
/// <c>mods,dlc,base</c>.
/// </summary>
/// <remarks>
/// <para>A file is read from the first layer that has it; listings merge the layers, the first entry of a path
/// wins. The layers are looked up on each call, so a layer mounted later, or remounted, takes part at once, and a
/// layer not mounted is skipped. Watches cover the layers mounted when the watch starts.</para>
/// <para>The option <c>write=&lt;layer&gt;</c> sends writes to that layer; without it the overlay is read-only. A
/// file of a lower layer opened to be changed is first copied up to it. A delete removes the copy of the write
/// layer only: the file of a lower layer shows again.</para>
/// </remarks>
public sealed class OverlayDriver()
    : Quant<MetaQuant>(new MetaQuant()), IFileDriver
{
    /// <inheritdoc/>
    public DomainScheme Scheme => DomainScheme.Overlay;

    /// <inheritdoc/>
    public IDomain Mount(DomainMount mount, IFileSystem fileSystem) => new OverlayDomain(mount, fileSystem);
}

internal sealed class OverlayDomain
    : DomainBase
{
    private readonly string[] _layers;
    private readonly string? _writeLayer;

    private static DomainCapabilities CapabilitiesOf(DomainMount mount)
    {
        var capabilities = DomainCapabilities.Read
                           | DomainCapabilities.Enumerate
                           | DomainCapabilities.Watch
                           | DomainCapabilities.RandomAccess
                           | DomainCapabilities.Memory;
        return mount.Options?.ContainsKey("write") == true
            ? capabilities | DomainCapabilities.Write | DomainCapabilities.AtomicWrite
            : capabilities;
    }

    public OverlayDomain(DomainMount mount, IFileSystem fileSystem)
        : base(mount, fileSystem, CapabilitiesOf(mount))
    {
        _layers = (mount.Source ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(layer => FileSystemNames.NormalizeName(layer, nameof(mount)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (_layers.Length == 0)
            throw new ArgumentException($"The overlay '{Name}' names no layers: " +
                                        $"expected a source such as mods,base", nameof(mount));
        if (_layers.Contains(Name))
            throw new ArgumentException($"The overlay '{Name}' cannot be a layer of itself", nameof(mount));

        if (!Options.TryGetValue("write", out var write)) return;

        _writeLayer = FileSystemNames.NormalizeName(write, nameof(mount));
        if (!_layers.Contains(_writeLayer))
            throw new ArgumentException($"The write layer '{write}' " +
                                        $"is not a layer of the overlay '{Name}'", nameof(mount));
    }

    private IEnumerable<IDomain> Layers()
    {
        foreach (var name in _layers)
            if (FileSystem.Domains.TryGet(name, out var layer))
                yield return layer;
    }

    private IDomain WriteLayer() =>
        _writeLayer is not null && FileSystem.Domains.TryGet(_writeLayer, out var layer)
            ? layer
            : throw new InvalidOperationException(
                $"The write layer '{_writeLayer}' of the overlay '{Name}' is not mounted");

    // The write layer gets its own copy of a file of a lower layer before changing it
    private void CopyUp(string path, IDomain layer, bool optional = false)
    {
        var lower = Layers().FirstOrDefault(l => !ReferenceEquals(l, layer) && l.Exists(path));
        if (lower is null)
        {
            if (optional) return;
            throw NotFound(path);
        }

        if (lower.TryGetEntry(path, out var entry) && entry.IsDirectory)
            throw Unsupported("change a directory of a lower layer");

        using var source = lower.Open(path, FileOpenOptions.Read);
        using var target = layer.Open(path, FileOpenOptions.Write);
        source.CopyTo(target);
    }

    private IDomain WriteLayerFor(string path, FileOpenOptions options)
    {
        var layer = WriteLayer();
        if (options.Mode is FileMode.Open or FileMode.OpenOrCreate or FileMode.Append && !layer.Exists(path))
            CopyUp(path, layer, optional: true);
        return layer;
    }

    private IDomain ReadLayerOf(string path) =>
        Layers().FirstOrDefault(layer => layer.Exists(path)) ?? throw NotFound(path);

    private sealed class Watches(List<IDisposable> watches)
        : IDisposable
    {
        public void Dispose()
        {
            foreach (var watch in watches) watch.Dispose();
        }
    }

    private sealed class Renamed(FilePath path, IRandomAccessFile file)
        : IRandomAccessFile
    {
        public FilePath Path { get; } = path;

        public long Length => file.Length;

        public int Read(long offset, Span<byte> buffer) => file.Read(offset, buffer);

        public ValueTask<int> ReadAsync(long offset, Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            file.ReadAsync(offset, buffer, cancellationToken);

        public void Dispose() => file.Dispose();
    }

    protected override bool ExistsCore(string path) =>
        Layers().Any(layer => layer.Exists(path));

    protected override bool TryGetEntryCore(string path, out FileEntry entry)
    {
        foreach (var layer in Layers())
        {
            if (!layer.TryGetEntry(path, out entry)) continue;
            entry = entry with { Path = entry.Path.WithDomain(Name) };
            return true;
        }

        entry = default;
        return false;
    }

    protected override IEnumerable<FileEntry> EnumerateCore(string directory, string pattern, bool recursive)
    {
        var layers = Layers()
            .Where(layer => layer.TryGetEntry(directory, out var entry) && entry.IsDirectory)
            .ToArray();
        if (layers.Length == 0)
            throw DirectoryNotFound(directory);

        return Merge();

        IEnumerable<FileEntry> Merge()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var layer in layers)
            foreach (var entry in layer.Enumerate(directory, pattern, recursive))
                if (seen.Add(entry.Path.Value))
                    yield return entry with { Path = entry.Path.WithDomain(Name) };
        }
    }

    protected override Stream OpenCore(string path, FileOpenOptions options) =>
        options.Writes ? WriteLayerFor(path, options).Open(path, options) : ReadLayerOf(path).Open(path, options);

    protected override ValueTask<Stream> OpenAsyncCore(string path, FileOpenOptions options,
        CancellationToken cancellationToken) =>
        options.Writes
            ? WriteLayerFor(path, options).OpenAsync(path, options, cancellationToken)
            : ReadLayerOf(path).OpenAsync(path, options, cancellationToken);

    protected override IRandomAccessFile OpenRandomAccessCore(string path) =>
        new Renamed(PathOf(path), ReadLayerOf(path).OpenRandomAccess(path));

    protected override bool TryGetMemoryCore(string path, out ReadOnlyMemory<byte> data)
    {
        data = default;
        var layer = Layers().FirstOrDefault(l => l.Exists(path));
        return layer is not null && layer.TryGetMemory(path, out data);
    }

    protected override void CreateDirectoryCore(string path) => WriteLayer().CreateDirectory(path);

    protected override bool DeleteCore(string path, bool recursive) => WriteLayer().Delete(path, recursive);

    protected override void MoveCore(string source, string destination, bool overwrite)
    {
        var layer = WriteLayer();
        if (!layer.Exists(source)) CopyUp(source, layer);
        layer.Move(source, destination, overwrite);
    }

    protected override IDisposable WatchCore(string directory, bool recursive, Action<FileChange> handler)
    {
        var watches = new List<IDisposable>();
        foreach (var layer in Layers())
        {
            if ((layer.Capabilities & DomainCapabilities.Watch) == 0) continue;
            if (!layer.TryGetEntry(directory, out var entry) || !entry.IsDirectory) continue;

            watches.Add(layer.Watch(directory, recursive, change => handler(change with
            {
                Path = change.Path.IsEmpty ? change.Path : change.Path.WithDomain(Name),
                OldPath = change.OldPath.IsEmpty ? change.OldPath : change.OldPath.WithDomain(Name)
            })));
        }

        return new Watches(watches);
    }
}
