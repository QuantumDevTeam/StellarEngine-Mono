// ReSharper disable RedundantNameQualifier

using Stellar.Kernel.FileSystem;
using Stellar.Kernel.FileSystem.Provider;
using Stellar.Core.Quantization;

namespace Stellar.FileSystem.Providers;

public class FileSystemFileProvider()
    : Quant<MetaQuant>(new MetaQuant()), IFileProvider
{
    #region Deps

    private string GetFullPath(IPath path) => System.IO.Path.Combine(path.Domain.Value, path.Value);

    public class FSFileInfo(System.IO.FileInfo fileInfo) : IFileInfo
    {
        public string Name { get; } = fileInfo.Name;
        public string FullPath { get; } = fileInfo.FullName;
        public long Length { get; } = fileInfo.Length;
        public DateTime? CreationTimeUtc { get; } = fileInfo.CreationTimeUtc;
        public DateTime? LastWriteTimeUtc { get; } = fileInfo.LastWriteTimeUtc;
    }

    #endregion

    private static readonly Lock _lock = new();

    public bool CanHandle(IDomain domain) => domain.Type == DomainType.Directory;

    public bool Exists(IPath path) => System.IO.File.Exists(GetFullPath(path));

    public IFileInfo GetFileInfo(IPath path) => new FSFileInfo(new System.IO.FileInfo(GetFullPath(path)));

    public Stream OpenRead(IPath path)
    {
        lock (_lock)
        {
            var stream = System.IO.File.OpenRead(GetFullPath(path));

            return stream is { CanRead: true }
                ? stream
                : throw new FileNotFoundException(
                    $"Resource `{path}` not found or can not be opened for reading.");
        }
    }

    public Stream OpenWrite(IPath path)
    {
        lock (_lock)
        {
            var stream = System.IO.File.OpenWrite(GetFullPath(path));

            return stream is { CanWrite: true }
                ? stream
                : throw new FileNotFoundException(
                    $"Resource `{path}` not found or can not be opened for writing.");
        }
    }

    public Stream OpenReadWrite(IPath path)
    {
        lock (_lock)
        {
            var stream = System.IO.File.Open(GetFullPath(path), FileMode.OpenOrCreate, FileAccess.ReadWrite);

            return stream is { CanRead: true, CanWrite: true }
                ? stream
                : throw new FileNotFoundException(
                    $"Resource `{path}` not found or can not be opened for reading and writing.");
        }
    }

    public Stream Open(IPath path, FileAccess access)
    {
        return access switch
        {
            FileAccess.Read => OpenRead(path),
            FileAccess.Write => OpenWrite(path),
            FileAccess.ReadWrite => OpenReadWrite(path),
            _ => throw new ArgumentOutOfRangeException(nameof(access), access, null)
        };
    }
}