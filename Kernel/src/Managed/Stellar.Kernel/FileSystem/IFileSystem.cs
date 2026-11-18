using System;
using Stellar.Kernel.Data.Catalog;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The files of an engine instance: drivers, the domains they mounted and the content types, behind
/// <see cref="FilePath"/>.
/// </summary>
/// <remarks>
/// <para>Each engine instance has its own file system; nothing of it is static. Code above names domains
/// (<c>assets://</c>, <c>user://</c>), never locations of the machine: those are given to drivers once, when the
/// domains are mounted.</para>
/// <para>Three catalogs: the drivers by scheme, the domains by name, the content types by extension. The file
/// system routes a path to its domain; the operations on paths are the extensions of
/// <see cref="FileSystemExtensions"/>. The data of files, cached and shared, is the business of the resource system
/// (<see cref="Resources.IResourceSystem"/>), built on this one.</para>
/// <para>Every member is safe to call from any thread.</para>
/// </remarks>
public interface IFileSystem
    : IQuant, IDisposable
{
    /// <summary>
    /// Gets the drivers, by the scheme they mount. A driver is set under its own <see cref="IFileDriver.Scheme"/>.
    /// </summary>
    ICatalog<DomainScheme, IFileDriver> Drivers { get; }

    /// <summary>
    /// Gets the domains mounted now, by name, in any case. <see cref="IReadOnlyCatalog{TKey,TValue}.Get"/> throws
    /// <see cref="DomainNotFoundException"/>. Domains come and go through <see cref="Mount"/> and <see cref="Unmount"/>.
    /// </summary>
    IReadOnlyCatalog<string, IDomain> Domains { get; }

    /// <summary>
    /// Gets the content types of this file system, by extension.
    /// </summary>
    IContentTypeCatalog ContentTypes { get; }

    /// <summary>
    /// Raised after a domain is mounted.
    /// </summary>
    event Action<IDomain>? Mounted;

    /// <summary>
    /// Raised before a domain is unmounted, while it still works: caches drop what they hold of it.
    /// </summary>
    event Action<IDomain>? Unmounting;

    /// <summary>
    /// Mounts a domain with the driver of its scheme.
    /// </summary>
    /// <param name="mount">What to mount.</param>
    /// <returns>The mounted domain.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a domain of that name is mounted, or no driver has the scheme.
    /// </exception>
    IDomain Mount(DomainMount mount);

    /// <summary>
    /// Unmounts a domain and disposes it.
    /// </summary>
    /// <param name="name">The name of the domain.</param>
    /// <returns><c>true</c> if the domain was mounted.</returns>
    bool Unmount(string name);
}
