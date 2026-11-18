using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// Mounts the domains of one scheme: directories, assembly resources, archives, a network, a world of a game.
/// </summary>
/// <remarks>
/// <para>A driver is registered in a file system (<see cref="IFileSystem.Drivers"/>, <see cref="FileSystemExtensions.AddDriver"/>), and the file system
/// picks it by <see cref="Scheme"/> when a domain is mounted: one lookup, once, not a question per file.</para>
/// <para>The driver keeps no state of its domains: what a domain needs (its root, an open archive, a connection)
/// lives in the <see cref="IDomain"/> it returns. One driver may serve several file systems, so several engine
/// instances.</para>
/// </remarks>
public interface IFileDriver
    : IQuant
{
    /// <summary>
    /// Gets the scheme of the domains this driver mounts.
    /// </summary>
    DomainScheme Scheme { get; }

    /// <summary>
    /// Mounts a domain.
    /// </summary>
    /// <param name="mount">What to mount; its scheme is <see cref="Scheme"/>.</param>
    /// <param name="fileSystem">
    /// The file system mounting it: for drivers built on other domains (an archive inside a domain, an overlay of
    /// domains) and for <see cref="IFileSystem.ContentTypes"/>.
    /// </param>
    /// <returns>The mounted domain.</returns>
    IDomain Mount(DomainMount mount, IFileSystem fileSystem);
}
