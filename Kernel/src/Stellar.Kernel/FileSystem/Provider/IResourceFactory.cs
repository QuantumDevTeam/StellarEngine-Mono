using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem.Provider
{
    /// <summary>
    /// Создаёт экземпляр <see cref="IResource"/> из сырых данных и метаданных файла.
    /// </summary>
    public interface IResourceFactory
        : IQuant
    {
        /// <summary>Создаёт ресурс из байтового массива.</summary>
        IResource CreateResource(IFile file, byte[] data);

        /// <summary>Создаёт ресурс из потока (читает все байты).</summary>
        IResource CreateResource(IFile file, Stream dataStream);

        /// <summary>Асинхронное создание ресурса из потока.</summary>
        Task<IResource> CreateResourceAsync(IFile file, Stream dataStream,
            CancellationToken cancellationToken = default);
    }
}