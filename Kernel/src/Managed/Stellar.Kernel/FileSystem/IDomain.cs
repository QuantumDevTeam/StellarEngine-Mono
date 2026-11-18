using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// A mounted domain: the files of one source (a directory, an assembly, an archive, a network) under one name.
/// </summary>
/// <remarks>
/// <para>A domain is what its driver made of a <see cref="DomainMount"/>, and it holds all the state of that
/// source: there is no lock or cache shared between domains, file systems or engine instances.</para>
/// <para>Paths are <see cref="FilePath.Value"/>: relative to the root of the domain and already in the one form of
/// <see cref="FileSystemNames"/>, so a domain does not check them again and nothing it is given leaves its root.
/// A domain over a directory still refuses links that lead out of it.</para>
/// <para>Every member is safe to call from any thread. A member the domain cannot do
/// (<see cref="Capabilities"/>) throws <see cref="NotSupportedException"/>.</para>
/// <para>Unmounting (<see cref="IFileSystem.Unmount"/>) disposes the domain; streams it opened stay usable until
/// they are closed.</para>
/// </remarks>
public interface IDomain
    : IQuant, IDisposable
{
    /// <summary>
    /// Gets the name of the domain, in <see cref="FilePath.Domain"/>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the scheme of the driver that mounted it.
    /// </summary>
    DomainScheme Scheme { get; }

    /// <summary>
    /// Gets where the driver found it, in the terms of the driver (<see cref="DomainMount.Source"/>).
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets what the domain can do.
    /// </summary>
    DomainCapabilities Capabilities { get; }

    /// <summary>
    /// Gets whether a file or a directory exists.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <returns><c>true</c> if there is a file or a directory at <paramref name="path"/>.</returns>
    bool Exists(string path);

    /// <summary>
    /// Reads what is known of a file or a directory.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <param name="entry">The entry, with its <see cref="FileEntry.Content"/> filled.</param>
    /// <returns><c>true</c> if there is a file or a directory at <paramref name="path"/>.</returns>
    bool TryGetEntry(string path, out FileEntry entry);

    /// <summary>
    /// Lists the entries of a directory. Needs <see cref="DomainCapabilities.Enumerate"/>.
    /// </summary>
    /// <param name="directory">The directory inside the domain; an empty string for the root.</param>
    /// <param name="pattern">The names to list: <c>*</c> for any run of characters, <c>?</c> for one.</param>
    /// <param name="recursive">Whether to go into the directories inside.</param>
    /// <returns>The entries, as they are found; files and directories alike.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if there is no such directory.</exception>
    IEnumerable<FileEntry> Enumerate(string directory, string pattern = "*", bool recursive = false);

    /// <summary>
    /// Opens a file as a stream. Writing needs <see cref="DomainCapabilities.Write"/>.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <param name="options">How to open it.</param>
    /// <returns>The stream; the caller closes it.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file must exist and does not.</exception>
    Stream Open(string path, FileOpenOptions options);

    /// <summary>
    /// Opens a file as a stream, without blocking on a slow source. Writing needs <see cref="DomainCapabilities.Write"/>.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <param name="options">How to open it.</param>
    /// <param name="cancellationToken">Stops the opening.</param>
    /// <returns>The stream; the caller closes it.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file must exist and does not.</exception>
    ValueTask<Stream> OpenAsync(string path, FileOpenOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a file for reads at any offset. Needs <see cref="DomainCapabilities.RandomAccess"/>.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <returns>The file; the caller disposes it.</returns>
    /// <exception cref="FileNotFoundException">Thrown if there is no such file.</exception>
    IRandomAccessFile OpenRandomAccess(string path);

    /// <summary>
    /// Gets the bytes of a file the domain holds in memory, without a copy.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <param name="data">The bytes; they never change: a write gives the file new memory.</param>
    /// <returns><c>true</c> if the domain holds the file in memory; <c>false</c> otherwise, then open it.</returns>
    bool TryGetMemory(string path, out ReadOnlyMemory<byte> data);

    /// <summary>
    /// Creates a directory and the directories above it. Needs <see cref="DomainCapabilities.Write"/>.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Deletes a file or a directory. Needs <see cref="DomainCapabilities.Write"/>.
    /// </summary>
    /// <param name="path">The path inside the domain.</param>
    /// <param name="recursive">Whether a directory goes with what it holds; without it, only an empty one goes.</param>
    /// <returns><c>true</c> if something was deleted; <c>false</c> if there was nothing.</returns>
    bool Delete(string path, bool recursive = false);

    /// <summary>
    /// Moves or renames a file or a directory inside the domain. Needs <see cref="DomainCapabilities.Write"/>.
    /// </summary>
    /// <param name="source">The path to move.</param>
    /// <param name="destination">The new path.</param>
    /// <param name="overwrite">Whether a file at <paramref name="destination"/> is replaced.</param>
    void Move(string source, string destination, bool overwrite = false);

    /// <summary>
    /// Reports the changes under a directory. Needs <see cref="DomainCapabilities.Watch"/>.
    /// </summary>
    /// <param name="directory">The directory inside the domain; an empty string for the whole domain.</param>
    /// <param name="recursive">Whether the directories inside are watched too.</param>
    /// <param name="handler">
    /// Called for each change, from any thread and possibly at once from several: it must be quick and safe.
    /// </param>
    /// <returns>Stops the watch when disposed.</returns>
    IDisposable Watch(string directory, bool recursive, Action<FileChange> handler);
}
