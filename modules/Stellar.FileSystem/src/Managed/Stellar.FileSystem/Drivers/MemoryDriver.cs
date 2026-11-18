using Stellar.Core.Quantization;
using Stellar.FileSystem.Domains;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Drivers;

/// <summary>
/// Mounts domains held in memory (<c>mem</c>): tests, generated data, scratch space. The source is not used.
/// </summary>
public sealed class MemoryDriver()
    : Quant<MetaQuant>(new MetaQuant()), IFileDriver
{
    /// <inheritdoc/>
    public DomainScheme Scheme => DomainScheme.Memory;

    /// <inheritdoc/>
    public IDomain Mount(DomainMount mount, IFileSystem fileSystem) => new MemoryDomain(mount, fileSystem);
}

/// <summary>
/// Files in memory. A write replaces the bytes of a file when its stream is closed, so readers and
/// <see cref="IDomain.TryGetMemory"/> never see a half-written file, and the memory they hold never changes.
/// </summary>
public sealed class MemoryDomain
    : DomainBase
{
    private sealed record MemoryFile(byte[] Data, DateTime CreationTimeUtc, DateTime LastWriteTimeUtc, long Revision);

    private sealed record Watcher(string Directory, bool Recursive, Action<FileChange> Handler);

    private readonly Lock _sync = new();

    private readonly Dictionary<string, MemoryFile> _files = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal) { string.Empty };
    private volatile Watcher[] _watchers = [];
    private long _revision;

    /// <summary>
    /// Makes the domain, empty.
    /// </summary>
    public MemoryDomain(DomainMount mount, IFileSystem fileSystem)
        : base(mount, fileSystem,
            DomainCapabilities.Read
            | DomainCapabilities.Write
            | DomainCapabilities.Enumerate
            | DomainCapabilities.Watch
            | DomainCapabilities.RandomAccess
            | DomainCapabilities.Memory
            | DomainCapabilities.AtomicWrite)
    {
    }

    // Under the lock. A file where a directory must be is an error, as on a disk
    private bool AddDirectories(string path)
    {
        if (path.Length == 0 || _directories.Contains(path)) return false;
        if (_files.ContainsKey(path)) throw new IOException($"'{PathOf(path)}' is a file");

        foreach (var ancestor in DomainPaths.AncestorsOf(path))
        {
            if (_files.ContainsKey(ancestor)) throw new IOException($"'{PathOf(ancestor)}' is a file");
            if (!_directories.Add(ancestor)) break;
        }

        _directories.Add(path);
        return true;
    }

    private void Notify(FileChange change)
    {
        // Out of the lock: a handler may call the domain
        foreach (var watcher in _watchers)
        {
            if (DomainPaths.IsIn(change.Path.Value, watcher.Directory, watcher.Recursive)
                || (!change.OldPath.IsEmpty &&
                    DomainPaths.IsIn(change.OldPath.Value, watcher.Directory, watcher.Recursive)))
                watcher.Handler(change);
        }
    }

    private void Commit(string path, byte[] data)
    {
        FileChangeKind kind;
        lock (_sync)
        {
            if (IsDisposed) return;
            if (_directories.Contains(path))
                throw new UnauthorizedAccessException($"'{PathOf(path)}' became a directory");

            var now = DateTime.UtcNow;
            var existed = _files.TryGetValue(path, out var old);
            AddDirectories(DomainPaths.ParentOf(path));
            _files[path] = new MemoryFile(data, old?.CreationTimeUtc ?? now, now, ++_revision);
            kind = existed ? FileChangeKind.Changed : FileChangeKind.Created;
        }

        Notify(new FileChange(kind, PathOf(path)));
    }

    private sealed class Unwatch(MemoryDomain domain, Watcher watcher)
        : IDisposable
    {
        public void Dispose()
        {
            lock (domain._sync) domain._watchers = domain._watchers.Where(w => !ReferenceEquals(w, watcher)).ToArray();
        }
    }

    // The new bytes become the file when the stream closes, at once
    private sealed class CommitStream
        : MemoryStream
    {
        private readonly MemoryDomain _domain;
        private readonly string _path;
        private bool _committed;

        public CommitStream(MemoryDomain domain, string path, byte[] initial, bool append)
        {
            _domain = domain;
            _path = path;
            Write(initial);
            Position = append ? Length : 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_committed)
            {
                _committed = true;
                _domain.Commit(_path, ToArray());
            }

            base.Dispose(disposing);
        }
    }

    /// <inheritdoc/>
    protected override bool ExistsCore(string path)
    {
        lock (_sync) return _files.ContainsKey(path) || _directories.Contains(path);
    }

    /// <inheritdoc/>
    protected override bool TryGetEntryCore(string path, out FileEntry entry)
    {
        lock (_sync)
        {
            if (_files.TryGetValue(path, out var file))
            {
                entry = FileEntryOf(path, file.Data.Length, file.CreationTimeUtc, file.LastWriteTimeUtc, file.Revision);
                return true;
            }

            entry = _directories.Contains(path) ? DirectoryEntryOf(path) : default;
            return !entry.Path.IsEmpty;
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<FileEntry> EnumerateCore(string directory, string pattern, bool recursive)
    {
        var entries = new List<FileEntry>();
        lock (_sync)
        {
            if (!_directories.Contains(directory)) throw DirectoryNotFound(directory);

            foreach (var path in _directories)
                if (DomainPaths.IsIn(path, directory, recursive) &&
                    DomainPaths.Matches(pattern, DomainPaths.NameOf(path)))
                    entries.Add(DirectoryEntryOf(path));

            foreach (var (path, file) in _files)
                if (DomainPaths.IsIn(path, directory, recursive) &&
                    DomainPaths.Matches(pattern, DomainPaths.NameOf(path)))
                    entries.Add(FileEntryOf(path, file.Data.Length, file.CreationTimeUtc, file.LastWriteTimeUtc,
                        file.Revision));
        }

        entries.Sort((left, right) => left.Path.CompareTo(right.Path));
        return entries;
    }

    /// <inheritdoc/>
    protected override Stream OpenCore(string path, FileOpenOptions options)
    {
        MemoryFile? file;
        lock (_sync)
        {
            if (_directories.Contains(path))
                throw new UnauthorizedAccessException($"'{PathOf(path)}' is a directory");
            _files.TryGetValue(path, out file);
        }

        switch (options.Mode)
        {
            case FileMode.Open or FileMode.Truncate when file is null:
                throw NotFound(path);
            case FileMode.CreateNew when file is not null:
                throw new IOException($"'{PathOf(path)}' exists already");
        }

        if (!options.Writes)
            return new MemoryStream(file!.Data, 0, file.Data.Length, writable: false, publiclyVisible: true);

        var keep = file is not null && options.Mode is FileMode.Open or FileMode.OpenOrCreate or FileMode.Append;
        return new CommitStream(this, path, keep ? file!.Data : [], options.Mode == FileMode.Append);
    }

    /// <inheritdoc/>
    protected override bool TryGetMemoryCore(string path, out ReadOnlyMemory<byte> data)
    {
        lock (_sync)
        {
            var found = _files.TryGetValue(path, out var file);
            data = found ? file!.Data : null;
            return found;
        }
    }

    /// <inheritdoc/>
    protected override IRandomAccessFile OpenRandomAccessCore(string path) =>
        TryGetMemoryCore(path, out var data)
            ? new MemoryRandomAccessFile(PathOf(path), data)
            : throw NotFound(path);

    /// <inheritdoc/>
    protected override void CreateDirectoryCore(string path)
    {
        bool created;
        lock (_sync) created = AddDirectories(path);
        if (created) Notify(new FileChange(FileChangeKind.Created, PathOf(path)));
    }

    /// <inheritdoc/>
    protected override bool DeleteCore(string path, bool recursive)
    {
        lock (_sync)
        {
            if (!_files.Remove(path))
            {
                if (!_directories.Contains(path)) return false;

                var inside = _files.Keys.Where(file => DomainPaths.IsIn(file, path, true))
                    .Concat(_directories.Where(directory => DomainPaths.IsIn(directory, path, true)))
                    .ToArray();
                if (inside.Length > 0 && !recursive)
                    throw new IOException($"'{PathOf(path)}' is not empty");

                foreach (var item in inside)
                    if (!_files.Remove(item))
                        _directories.Remove(item);
                _directories.Remove(path);
            }
        }

        Notify(new FileChange(FileChangeKind.Deleted, PathOf(path)));
        return true;
    }

    /// <inheritdoc/>
    protected override void MoveCore(string source, string destination, bool overwrite)
    {
        lock (_sync)
        {
            if (_files.TryGetValue(source, out var file))
            {
                if (_directories.Contains(destination))
                    throw new IOException($"'{PathOf(destination)}' is a directory");
                if (_files.ContainsKey(destination) && !overwrite)
                    throw new IOException($"'{PathOf(destination)}' exists already");

                AddDirectories(DomainPaths.ParentOf(destination));
                _files.Remove(source);
                _files[destination] = file with { Revision = ++_revision };
            }
            else if (_directories.Contains(source))
            {
                if (_files.ContainsKey(destination) || _directories.Contains(destination))
                    throw new IOException($"'{PathOf(destination)}' exists already");

                AddDirectories(DomainPaths.ParentOf(destination));
                var prefix = source + "/";
                foreach (var directory in _directories
                             .Where(d => d == source || d.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
                {
                    _directories.Remove(directory);
                    _directories.Add(destination + directory.Substring(source.Length));
                }

                foreach (var (path, moved) in _files.Where(f => f.Key.StartsWith(prefix, StringComparison.Ordinal))
                             .ToArray())
                {
                    _files.Remove(path);
                    _files[destination + path.Substring(source.Length)] = moved with { Revision = ++_revision };
                }
            }
            else throw NotFound(source);
        }

        Notify(new FileChange(FileChangeKind.Renamed, PathOf(destination), PathOf(source)));
    }

    /// <inheritdoc/>
    protected override IDisposable WatchCore(string directory, bool recursive, Action<FileChange> handler)
    {
        var watcher = new Watcher(directory, recursive, handler);
        lock (_sync)
        {
            if (!_directories.Contains(directory))
                throw DirectoryNotFound(directory);
            _watchers = [.. _watchers, watcher];
        }

        return new Unwatch(this, watcher);
    }

    /// <inheritdoc/>
    protected override void DisposeCore()
    {
        lock (_sync)
        {
            _watchers = [];
            _files.Clear();
        }
    }
}
