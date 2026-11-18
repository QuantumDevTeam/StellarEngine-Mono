using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stellar.Kernel.FileSystem.Resources;

/// <summary>
/// A lease on the data of a file: what the file system finds, the resource system holds and shares.
/// </summary>
/// <remarks>
/// <para>A file is an address and what is known of it (<see cref="FilePath"/>, <see cref="FileEntry"/>); a resource
/// is its data. Every lease counts as a reference: the resource system frees the data when the last lease is
/// disposed, as <see cref="Policy"/> says. Dispose each lease once.</para>
/// <para>A lease is a snapshot: its data stays the same for its whole life, even when the file changes. Then
/// <see cref="IsStale"/> becomes <c>true</c> and <see cref="Changed"/> is raised, and whoever wants the new data
/// acquires the resource again. Under <see cref="ResourcePolicy.Stream"/> nothing is held, so each
/// <see cref="Open"/> reads the file as it is now.</para>
/// <para>What the data becomes (a texture, a level) is decided above, by the asset system: a resource is bytes
/// with a <see cref="ContentType"/>.</para>
/// </remarks>
public interface IResource
    : IDisposable
{
    /// <summary>
    /// Gets the path of the file.
    /// </summary>
    FilePath Path { get; }

    /// <summary>
    /// Gets what was known of the file when the data of this lease was read.
    /// </summary>
    FileEntry Entry { get; }

    /// <summary>
    /// Gets how the data is held.
    /// </summary>
    ResourcePolicy Policy { get; }

    /// <summary>
    /// Gets the version of the data of this lease: it grows each time the resource system reads the file anew.
    /// </summary>
    long Version { get; }

    /// <summary>
    /// Gets whether the file changed after the data of this lease was read.
    /// </summary>
    bool IsStale { get; }

    /// <summary>
    /// Raised once, when the file changes after the data of this lease was read; from any thread.
    /// </summary>
    event Action<IResource>? Changed;

    /// <summary>
    /// Opens the data as a stream, from its start.
    /// </summary>
    /// <returns>A stream of its own, read-only; the caller closes it.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the lease is disposed.</exception>
    Stream Open();

    /// <summary>
    /// Opens the data as a stream, from its start, without blocking on a slow source.
    /// </summary>
    /// <param name="cancellationToken">Stops the opening.</param>
    /// <returns>A stream of its own, read-only; the caller closes it.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the lease is disposed.</exception>
    ValueTask<Stream> OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the data in memory, without a copy, when it is there.
    /// </summary>
    /// <param name="data">The bytes; valid while the lease lives.</param>
    /// <returns><c>true</c> if the data is in memory; <c>false</c> otherwise, then use <see cref="Open"/>.</returns>
    bool TryGetMemory(out ReadOnlyMemory<byte> data);

    /// <summary>
    /// Takes another lease on the same data, for another owner.
    /// </summary>
    /// <returns>A lease of its own, to dispose on its own.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the lease is disposed.</exception>
    IResource Share();
}
