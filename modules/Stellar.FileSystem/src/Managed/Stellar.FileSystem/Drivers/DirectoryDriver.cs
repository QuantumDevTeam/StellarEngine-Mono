using System.IO.Enumeration;
using Microsoft.Win32.SafeHandles;
using Stellar.Core.Quantization;
using Stellar.FileSystem.Domains;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem.Drivers;

/// <summary>
/// Mounts a directory of the machine (<c>dir</c>). The source is its full path, which the runtime resolves from
/// <c>$(StellarRoot)</c>, the project or the folders of the user; a relative one is refused.
/// </summary>
/// <remarks>
/// <para>Options:</para>
/// <list type="bullet">
/// <item><c>readonly=true</c>: no writes. A missing directory is then an error; otherwise it is created.</item>
/// <item><c>links=follow</c>: follows links wherever they lead. By default a link (symbolic link, junction) that
/// leads out of the directory is refused, and listings do not go into linked directories.</item>
/// </list>
/// <para>Atomic writes go to a file beside the target, named <c>*.stellar-tmp</c>, which listings and watches
/// never show.</para>
/// </remarks>
public sealed class DirectoryDriver()
    : Quant<MetaQuant>(new MetaQuant()), IFileDriver
{
    /// <inheritdoc/>
    public DomainScheme Scheme => DomainScheme.Directory;

    /// <inheritdoc/>
    public IDomain Mount(DomainMount mount, IFileSystem fileSystem) => new DirectoryDomain(mount, fileSystem);
}

/// <summary>
/// Writes beside a file and puts the result in its place when closed: the file is the old one or the new one,
/// never a part of the new one.
/// </summary>
internal sealed class AtomicFileStream
    : FileStream
{
    private readonly string _target;
    private readonly string _temporary;
    private readonly bool _overwrite;
    private int _committed;

    public AtomicFileStream(string target, bool overwrite, FileOptions options)
        : this(target, TemporaryOf(target), overwrite, options)
    {
    }

    private AtomicFileStream(string target, string temporary, bool overwrite, FileOptions options)
        : base(temporary,
            new FileStreamOptions
                { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None, Options = options })
    {
        _target = target;
        _temporary = temporary;
        _overwrite = overwrite;
    }

    private static string TemporaryOf(string target) =>
        Path.Combine(Path.GetDirectoryName(target)!,
            $".{Path.GetFileName(target)}.{Guid.NewGuid():N}{DirectoryDomain.TemporarySuffix}");

    protected override void Dispose(bool disposing)
    {
        // The bytes reach the disk before the file takes their place
        var commit = disposing && Volatile.Read(ref _committed) == 0 && CanWrite;
        if (commit) Flush(flushToDisk: true);
        base.Dispose(disposing);
        if (commit) Commit();
    }

    public override async ValueTask DisposeAsync()
    {
        if (Volatile.Read(ref _committed) != 0 || !CanWrite)
        {
            await base.DisposeAsync().ConfigureAwait(false);
            return;
        }

        await FlushAsync().ConfigureAwait(false);
        Flush(flushToDisk: true);
        await base.DisposeAsync().ConfigureAwait(false);
        Commit();
    }

    private void Commit()
    {
        if (Interlocked.Exchange(ref _committed, 1) != 0) return;

        try
        {
            File.Move(_temporary, _target, _overwrite);
        }
        catch
        {
            File.Delete(_temporary);
            throw;
        }
    }
}

internal sealed class DirectoryDomain
    : DomainBase
{
    internal const string TemporarySuffix = ".stellar-tmp";

    private static readonly bool IgnoreCase = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    private static readonly StringComparison PathComparison = IgnoreCase
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static readonly char Separator = Path.DirectorySeparatorChar;

    // With a separator at the end, so "C:\Game2" is not inside "C:\Game"
    private readonly string _root;
    private readonly bool _followLinks;

    private static DomainCapabilities CapabilitiesOf(DomainMount mount)
    {
        var capabilities = DomainCapabilities.Read | DomainCapabilities.Enumerate | DomainCapabilities.Watch |
                           DomainCapabilities.RandomAccess;
        return GetFlag(mount.Options, "readonly", false)
            ? capabilities
            : capabilities | DomainCapabilities.Write | DomainCapabilities.AtomicWrite;
    }

    public DirectoryDomain(DomainMount mount, IFileSystem fileSystem)
        : base(mount, fileSystem, CapabilitiesOf(mount))
    {
        if (string.IsNullOrWhiteSpace(mount.Source)
            || !Path.IsPathFullyQualified(mount.Source))
            throw new ArgumentException($"The source of the dir domain '{mount.Name}' must be a full path, " +
                                        $"resolved by the runtime: '{mount.Source}'", nameof(mount));

        var full = Path.GetFullPath(mount.Source).TrimEnd(Separator, Path.AltDirectorySeparatorChar);
        _root = full + Separator;
        _followLinks = Options.TryGetValue("links", out var links) && links == "follow";

        if (!Directory.Exists(full))
        {
            if ((Capabilities & DomainCapabilities.Write) == 0)
                throw new DirectoryNotFoundException($"No directory '{full}' for the domain '{Name}'");
            Directory.CreateDirectory(full);
        }
    }

    // The path of the machine; refuses a link on the way that leads out of the root
    private string Resolve(string path)
    {
        if (path.Length == 0) return _root;

        var full = _root + (Separator == '/' ? path : path.Replace('/', Separator));
        if (_followLinks) return full;

        for (var index = _root.Length;; index++)
        {
            index = full.IndexOf(Separator, index);
            var current = index < 0 ? full : full.Substring(0, index);

            var info = new FileInfo(current);
            var attributes = (int)info.Attributes;
            if (attributes == -1) break; // Nothing there, nor below
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0 && info.LinkTarget is not null
                                                                     && !IsInside(
                                                                         info.ResolveLinkTarget(true)?.FullName))
                throw new UnauthorizedAccessException(
                    $"'{PathOf(path)}' leads out of the domain '{Name}' through a link");

            if (index < 0) break;
        }

        return full;
    }

    private FileEntry EntryOf(string path, FileInfo file)
    {
        // The time of the last write, and the length for file systems that keep the time coarse
        var revision = unchecked((file.LastWriteTimeUtc.Ticks * 397) ^ file.Length);
        return FileEntryOf(path, file.Length, file.CreationTimeUtc, file.LastWriteTimeUtc, revision);
    }

    // The path inside the domain of a path of the machine; null for what cannot be one
    private string? RelativeOf(string full)
    {
        if (!(full + Separator).StartsWith(_root, PathComparison) || full.Length < _root.Length) return null;
        if (full.EndsWith(TemporarySuffix, StringComparison.Ordinal)) return null;

        var relative = full.Substring(_root.Length);
        if (Separator != '/') relative = relative.Replace(Separator, '/');
        if (Separator == '/' && relative.Contains('\\')) return null;

        // A name the engine would not accept (possible on some file systems) is not a file of the domain
        return FileSystemNames.TryNormalizePath(relative, out var normalized) && normalized == relative
            ? relative
            : null;
    }

    private bool IsInside(string? full) =>
        full is not null && (full + Separator).StartsWith(_root, PathComparison);

    private Stream OpenFile(string path, FileOpenOptions options, bool asynchronous)
    {
        var full = Resolve(path);
        if (Directory.Exists(full)) throw new UnauthorizedAccessException($"'{PathOf(path)}' is a directory");

        var fileOptions = asynchronous ? FileOptions.Asynchronous : FileOptions.None;
        if ((options.Flags & FileOpenFlags.Sequential) != 0) fileOptions |= FileOptions.SequentialScan;
        if ((options.Flags & FileOpenFlags.RandomAccess) != 0) fileOptions |= FileOptions.RandomAccess;

        if (!options.Writes)
        {
            if (!File.Exists(full)) throw NotFound(path);
            return new FileStream(full, new FileStreamOptions
            {
                Mode = options.Mode, Access = options.Access, Share = FileShare.ReadWrite | FileShare.Delete,
                Options = fileOptions
            });
        }

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        if ((options.Flags & FileOpenFlags.Atomic) != 0)
        {
            if (options.Mode == FileMode.CreateNew && File.Exists(full))
                throw new IOException($"'{PathOf(path)}' exists already");
            return new AtomicFileStream(full, options.Mode == FileMode.Create, fileOptions);
        }

        return new FileStream(full, new FileStreamOptions
        {
            Mode = options.Mode, Access = options.Access, Share = FileShare.Read, Options = fileOptions
        });
    }

    private sealed class RandomAccessFile(FilePath path, SafeFileHandle handle)
        : IRandomAccessFile
    {
        public FilePath Path { get; } = path;

        public long Length => RandomAccess.GetLength(handle);

        public int Read(long offset, Span<byte> buffer) => RandomAccess.Read(handle, buffer, offset);

        public ValueTask<int> ReadAsync(long offset, Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            RandomAccess.ReadAsync(handle, buffer, offset, cancellationToken);

        public void Dispose() => handle.Dispose();
    }

    protected override bool ExistsCore(string path)
    {
        try
        {
            var full = Resolve(path);
            return File.Exists(full) || Directory.Exists(full);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    protected override bool TryGetEntryCore(string path, out FileEntry entry)
    {
        entry = default;
        string full;
        try
        {
            full = Resolve(path);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        var file = new FileInfo(full);
        if (file.Exists)
        {
            entry = EntryOf(path, file);
            return true;
        }

        var directory = new DirectoryInfo(full);
        if (!directory.Exists) return false;

        entry = DirectoryEntryOf(path, directory.CreationTimeUtc, directory.LastWriteTimeUtc);
        return true;
    }

    protected override IEnumerable<FileEntry> EnumerateCore(string directory, string pattern, bool recursive)
    {
        var full = Resolve(directory);
        if (!Directory.Exists(full)) throw DirectoryNotFound(directory);

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,
            AttributesToSkip = 0,
            ReturnSpecialDirectories = false
        };

        var entries = new FileSystemEnumerable<FileSystemInfo>(full,
            (ref FileSystemEntry entry) => entry.ToFileSystemInfo(), options)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                !entry.FileName.EndsWith(TemporarySuffix, StringComparison.Ordinal)
                && DomainPaths.Matches(pattern, entry.FileName.ToString(), IgnoreCase),
            // Never through a link into another place
            ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                _followLinks || (entry.Attributes & FileAttributes.ReparsePoint) == 0
        };

        return Select(entries);

        IEnumerable<FileEntry> Select(IEnumerable<FileSystemInfo> infos)
        {
            foreach (var info in infos)
            {
                if (RelativeOf(info.FullName) is not { } path) continue;
                if (!_followLinks && info.LinkTarget is not null &&
                    !IsInside(info.ResolveLinkTarget(true)?.FullName)) continue;

                yield return info is FileInfo file
                    ? EntryOf(path, file)
                    : DirectoryEntryOf(path, info.CreationTimeUtc, info.LastWriteTimeUtc);
            }
        }
    }

    protected override Stream OpenCore(string path, FileOpenOptions options) =>
        OpenFile(path, options, asynchronous: false);

    protected override ValueTask<Stream> OpenAsyncCore(string path, FileOpenOptions options,
        CancellationToken cancellationToken) =>
        new(OpenFile(path, options, asynchronous: true));

    protected override IRandomAccessFile OpenRandomAccessCore(string path)
    {
        var full = Resolve(path);
        if (!File.Exists(full)) throw NotFound(path);

        var handle = File.OpenHandle(full, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            FileOptions.RandomAccess | FileOptions.Asynchronous);
        return new RandomAccessFile(PathOf(path), handle);
    }

    protected override void CreateDirectoryCore(string path) => Directory.CreateDirectory(Resolve(path));

    protected override bool DeleteCore(string path, bool recursive)
    {
        var full = Resolve(path);
        if (File.Exists(full))
        {
            File.Delete(full);
            return true;
        }

        if (!Directory.Exists(full)) return false;

        // A linked directory goes as a link: Directory.Delete does not walk into it
        Directory.Delete(full, recursive);
        return true;
    }

    protected override void MoveCore(string source, string destination, bool overwrite)
    {
        var from = Resolve(source);
        var to = Resolve(destination);
        Directory.CreateDirectory(Path.GetDirectoryName(to)!);

        if (File.Exists(from)) File.Move(from, to, overwrite);
        else if (Directory.Exists(from)) Directory.Move(from, to);
        else throw NotFound(source);
    }

    protected override IDisposable WatchCore(string directory, bool recursive, Action<FileChange> handler)
    {
        var full = Resolve(directory);
        if (!Directory.Exists(full)) throw DirectoryNotFound(directory);

        var watcher = new FileSystemWatcher(full)
        {
            IncludeSubdirectories = recursive,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite |
                           NotifyFilters.Size | NotifyFilters.CreationTime,
            InternalBufferSize = 64 * 1024
        };

        watcher.Created += (_, e) => Report(FileChangeKind.Created, e.FullPath);
        watcher.Deleted += (_, e) => Report(FileChangeKind.Deleted, e.FullPath);
        watcher.Changed += (_, e) =>
        {
            // A directory changes when what it holds changes: that is reported on its own
            if (!Directory.Exists(e.FullPath)) Report(FileChangeKind.Changed, e.FullPath);
        };
        watcher.Renamed += (_, e) =>
        {
            var path = RelativeOf(e.FullPath);
            if (path is null) return;

            // The end of an atomic write: the file got new content
            if (e.OldFullPath.EndsWith(TemporarySuffix, StringComparison.Ordinal))
            {
                handler(new FileChange(FileChangeKind.Changed, PathOf(path)));
                return;
            }

            var old = RelativeOf(e.OldFullPath);
            handler(old is null
                ? new FileChange(FileChangeKind.Created, PathOf(path))
                : new FileChange(FileChangeKind.Renamed, PathOf(path), PathOf(old)));
        };
        watcher.Error += (_, _) => handler(new FileChange(FileChangeKind.Invalidated, PathOf(directory)));
        watcher.EnableRaisingEvents = true;
        return watcher;

        void Report(FileChangeKind kind, string fullPath)
        {
            if (RelativeOf(fullPath) is { } path) handler(new FileChange(kind, PathOf(path)));
        }
    }
}
