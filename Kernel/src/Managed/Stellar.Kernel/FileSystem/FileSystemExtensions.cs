using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The operations of <see cref="IDomain"/> on full paths: each finds the domain of the path and calls it.
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    /// Gets the domain of a path.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The path.</param>
    /// <returns>The domain that <paramref name="path"/> names.</returns>
    /// <exception cref="ArgumentException">Thrown for the empty path.</exception>
    /// <exception cref="DomainNotFoundException">Thrown if the domain is not mounted.</exception>
    public static IDomain GetDomain(this IFileSystem fileSystem, FilePath path)
    {
        if (path.IsEmpty) throw new ArgumentException("The empty path names no domain", nameof(path));
        return fileSystem.Domains.Get(path.Domain);
    }

    /// <summary>
    /// Adds a driver under its scheme.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="driver">The driver.</param>
    /// <exception cref="InvalidOperationException">Thrown if another driver has the same scheme.</exception>
    public static void AddDriver(this IFileSystem fileSystem, IFileDriver driver)
    {
        if (driver is null) throw new ArgumentNullException(nameof(driver));
        if (!fileSystem.Drivers.TryAdd(driver.Scheme, driver))
            throw new InvalidOperationException($"A driver of the scheme '{driver.Scheme}' is there already");
    }

    /// <inheritdoc cref="IDomain.Exists"/>
    public static bool Exists(this IFileSystem fileSystem, FilePath path) =>
        !path.IsEmpty && fileSystem.Domains.TryGet(path.Domain, out var domain) && domain.Exists(path.Value);

    /// <inheritdoc cref="IDomain.TryGetEntry"/>
    public static bool TryGetEntry(this IFileSystem fileSystem, FilePath path, out FileEntry entry)
    {
        entry = default;
        return !path.IsEmpty
               && fileSystem.Domains.TryGet(path.Domain, out var domain)
               && domain.TryGetEntry(path.Value, out entry);
    }

    /// <summary>
    /// Reads what is known of a file or a directory.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The path.</param>
    /// <returns>The entry.</returns>
    /// <exception cref="FileNotFoundException">Thrown if there is nothing at <paramref name="path"/>.</exception>
    public static FileEntry GetEntry(this IFileSystem fileSystem, FilePath path) =>
        fileSystem.GetDomain(path).TryGetEntry(path.Value, out var entry)
            ? entry
            : throw new FileNotFoundException($"Nothing at '{path}'", path.ToString());

    /// <inheritdoc cref="IDomain.Enumerate"/>
    public static IEnumerable<FileEntry> Enumerate(this IFileSystem fileSystem, FilePath directory,
        string pattern = "*", bool recursive = false) =>
        fileSystem.GetDomain(directory).Enumerate(directory.Value, pattern, recursive);

    /// <inheritdoc cref="IDomain.Open"/>
    public static Stream Open(this IFileSystem fileSystem, FilePath path, FileOpenOptions options) =>
        fileSystem.GetDomain(path).Open(path.Value, options);

    /// <summary>
    /// Opens a file that exists for reading.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The path.</param>
    /// <returns>The stream; the caller closes it.</returns>
    public static Stream OpenRead(this IFileSystem fileSystem, FilePath path) =>
        fileSystem.GetDomain(path).Open(path.Value, FileOpenOptions.Read);

    /// <inheritdoc cref="IDomain.OpenAsync"/>
    public static ValueTask<Stream> OpenAsync(this IFileSystem fileSystem, FilePath path, FileOpenOptions options,
        CancellationToken cancellationToken = default) =>
        fileSystem.GetDomain(path).OpenAsync(path.Value, options, cancellationToken);

    /// <inheritdoc cref="IDomain.OpenRandomAccess"/>
    public static IRandomAccessFile OpenRandomAccess(this IFileSystem fileSystem, FilePath path) =>
        fileSystem.GetDomain(path).OpenRandomAccess(path.Value);

    /// <inheritdoc cref="IDomain.TryGetMemory"/>
    public static bool TryGetMemory(this IFileSystem fileSystem, FilePath path, out ReadOnlyMemory<byte> data)
    {
        data = default;
        return !path.IsEmpty
               && fileSystem.Domains.TryGet(path.Domain, out var domain)
               && domain.TryGetMemory(path.Value, out data);
    }

    /// <inheritdoc cref="IDomain.CreateDirectory"/>
    public static void CreateDirectory(this IFileSystem fileSystem, FilePath path) =>
        fileSystem.GetDomain(path).CreateDirectory(path.Value);

    /// <inheritdoc cref="IDomain.Delete"/>
    public static bool Delete(this IFileSystem fileSystem, FilePath path, bool recursive = false) =>
        fileSystem.GetDomain(path).Delete(path.Value, recursive);

    /// <summary>
    /// Moves or renames a file or a directory inside its domain.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="source">The path to move.</param>
    /// <param name="destination">The new path, in the same domain.</param>
    /// <param name="overwrite">Whether a file at <paramref name="destination"/> is replaced.</param>
    /// <exception cref="ArgumentException">Thrown if the paths are in different domains: copy, then delete.</exception>
    public static void Move(this IFileSystem fileSystem, FilePath source, FilePath destination, bool overwrite = false)
    {
        if (source.Domain != destination.Domain)
            throw new ArgumentException($"'{source}' and '{destination}' are in different domains", nameof(destination));
        fileSystem.GetDomain(source).Move(source.Value, destination.Value, overwrite);
    }

    /// <inheritdoc cref="IDomain.Watch"/>
    public static IDisposable Watch(this IFileSystem fileSystem, FilePath directory, bool recursive,
        Action<FileChange> handler) =>
        fileSystem.GetDomain(directory).Watch(directory.Value, recursive, handler);
}
