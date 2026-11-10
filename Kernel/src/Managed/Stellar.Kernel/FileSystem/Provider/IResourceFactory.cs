using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem.Provider;

/// <summary>
/// Creates an <see cref="IResource"/> from raw data and file metadata.
/// </summary>
public interface IResourceFactory
    : IQuant
{
    /// <summary>Creates a resource from a byte array.</summary>
    IResource CreateResource(IFile file, byte[] data);

    /// <summary>Creates a resource from a stream, reading all bytes.</summary>
    IResource CreateResource(IFile file, Stream dataStream);

    /// <summary>Creates a resource from a stream asynchronously.</summary>
    Task<IResource> CreateResourceAsync(IFile file, Stream dataStream,
        CancellationToken cancellationToken = default);
}
