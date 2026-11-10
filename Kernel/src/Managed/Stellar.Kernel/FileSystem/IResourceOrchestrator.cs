using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stellar.Kernel.FileSystem.Provider;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// Main coordinator of the file system: domain resolving, provider selection, factories, caching.
/// </summary>
public interface IResourceOrchestrator : IQuant
{
    // ----- Component registration -----

    /// <summary>Registers a provider for a domain type.</summary>
    void RegisterProvider(DomainType domainType, IFileProvider provider);

    /// <summary>Registers a factory for an extension (with or without the dot).</summary>
    void RegisterFactory(string extension, IResourceFactory factory);

    /// <summary>Mounts a domain (root or nested).</summary>
    void MountDomain(IDomain domain, IDomain? parentDomain = null);

    // ----- Resource loading (sync, async, streaming) -----

    /// <summary>Loads a resource synchronously (may block the thread).</summary>
    IResource Load(IPath path);

    /// <summary>Loads a resource asynchronously.</summary>
    Task<IResource> LoadAsync(IPath path, CancellationToken cancellationToken = default);

    /// <summary>Opens a stream to read a resource (streaming).</summary>
    Stream OpenStream(IPath path);

    /// <summary>Checks whether a resource is in the cache.</summary>
    bool IsLoaded(IPath path);

    /// <summary>Evicts a resource from the cache unless someone holds it.</summary>
    void Unload(IPath path);

    /// <summary>Gets the file metadata.</summary>
    IFileInfo GetFileInfo(IPath path);

    /// <summary>Checks whether a file exists.</summary>
    bool Exists(IPath path);
}
