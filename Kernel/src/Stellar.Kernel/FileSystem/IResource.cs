using System;
using System.Collections.ObjectModel;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem
{
    /// <summary>
    /// ресурс, выданные файловой системой движка
    /// </summary>
    public interface IResource
        : IMetaQuant, IEquatable<IResource>
    {
        /// <summary>Ссылка на файл (домен + путь).</summary>
        IFile File { get; }

        /// <summary>Сырые данные ресурса (загруженные байты).</summary>
        ReadOnlyCollection<byte> Data { get; }
    }
}