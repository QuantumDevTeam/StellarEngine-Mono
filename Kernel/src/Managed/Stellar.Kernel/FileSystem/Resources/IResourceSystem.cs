using System;
using System.Threading;
using System.Threading.Tasks;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem.Resources;

/// <summary>
/// The data of files, read once and shared: leases with reference counts, a cache, and reloads when files change.
/// </summary>
/// <remarks>
/// <para>Built on an <see cref="IFileSystem"/>: one per engine instance, like it. The file system answers where a
/// file is and what is known of it; the resource system holds its data.</para>
/// <para>It watches what it holds, where the domain can (<see cref="DomainCapabilities.Watch"/>), and coalesces the
/// bursts of changes: a changed file gets a new <see cref="IResource.Version"/> on its next acquisition, and the
/// leases on its old data become stale (<see cref="IResource.IsStale"/>). A domain being unmounted drops what is
/// held of it.</para>
/// <para>Every member is safe to call from any thread.</para>
/// </remarks>
public interface IResourceSystem
    : IQuant, IDisposable
{
    /// <summary>
    /// Gets the file system the data comes from.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// Gets or sets how many bytes the cache keeps for resources that no lease holds; 0 keeps none.
    /// </summary>
    long CacheBudget { get; set; }

    /// <summary>
    /// Gets how many bytes are held now, leased or not.
    /// </summary>
    long HeldBytes { get; }

    /// <summary>
    /// Raised when a file that is held changed, after the changes were coalesced; from any thread.
    /// </summary>
    event Action<FileChange>? Changed;

    /// <summary>
    /// Takes a lease on the data of a file, reading it if it is not held.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="policy">How to hold the data.</param>
    /// <returns>The lease; the caller disposes it.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if there is no such file.</exception>
    /// <exception cref="DomainNotFoundException">Thrown if the domain is not mounted.</exception>
    IResource Acquire(FilePath path, ResourcePolicy policy = ResourcePolicy.Cache);

    /// <summary>
    /// Takes a lease on the data of a file, reading it if it is not held, without blocking on a slow source.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <param name="policy">How to hold the data.</param>
    /// <param name="cancellationToken">Stops the reading; a read shared with other callers goes on for them.</param>
    /// <returns>The lease; the caller disposes it.</returns>
    /// <exception cref="System.IO.FileNotFoundException">Thrown if there is no such file.</exception>
    /// <exception cref="DomainNotFoundException">Thrown if the domain is not mounted.</exception>
    ValueTask<IResource> AcquireAsync(FilePath path, ResourcePolicy policy = ResourcePolicy.Cache, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the data of a file is held now, leased or cached.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns><c>true</c> if the next acquisition reads nothing.</returns>
    bool IsHeld(FilePath path);

    /// <summary>
    /// Forgets the data of a file, as if it had changed: the next acquisition reads it anew, and the leases on it
    /// become stale.
    /// </summary>
    /// <param name="path">The file, or a directory for everything under it.</param>
    void Invalidate(FilePath path);

    /// <summary>
    /// Frees the data that no lease holds.
    /// </summary>
    void Trim();
}
