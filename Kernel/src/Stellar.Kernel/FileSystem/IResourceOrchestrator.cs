using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stellar.Kernel.FileSystem.Provider;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.FileSystem
{
    /// <summary>
    /// Главный координатор файловой системы: резолвинг доменов, выбор провайдера, фабрики, кеширование.
    /// </summary>
    public interface IResourceOrchestrator : IQuant
    {
        // ----- Регистрация компонентов -----

        /// <summary>Регистрирует провайдера для заданного типа домена.</summary>
        void RegisterProvider(DomainType domainType, IFileProvider provider);

        /// <summary>Регистрирует фабрику для заданного расширения (с точкой или без).</summary>
        void RegisterFactory(string extension, IResourceFactory factory);

        /// <summary>Монтирует домен (корневой или вложенный).</summary>
        void MountDomain(IDomain domain, IDomain parentDomain = null);

        // ----- Загрузка ресурсов (синхронная, асинхронная, стриминг) -----

        /// <summary>Загружает ресурс синхронно (может блокировать поток).</summary>
        IResource Load(IPath path);

        /// <summary>Загружает ресурс асинхронно.</summary>
        Task<IResource> LoadAsync(IPath path, CancellationToken cancellationToken = default);

        /// <summary>Открывает поток для чтения ресурса (стриминг).</summary>
        Stream OpenStream(IPath path);

        /// <summary>Проверяет, загружен ли ресурс в кеш.</summary>
        bool IsLoaded(IPath path);

        /// <summary>Выгружает ресурс из кеша (если никто не держит).</summary>
        void Unload(IPath path);

        /// <summary>Получает информацию о файле (метаданные).</summary>
        IFileInfo GetFileInfo(IPath path);

        /// <summary>Проверяет существование файла.</summary>
        bool Exists(IPath path);
    }
}