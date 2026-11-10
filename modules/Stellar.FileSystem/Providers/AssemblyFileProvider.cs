using System.Reflection;
using Stellar.Kernel.FileSystem;
using Stellar.Kernel.FileSystem.Provider;
using Stellar.Core.Quantization;

namespace Stellar.FileSystem.Providers;

public class AssemblyFileProvider(Assembly assembly)
    : Quant<MetaQuant>(new MetaQuant()), IFileProvider
{
    #region Deps

    public class AFileInfo(string name, string path, long length) : IFileInfo
    {
        public string Name { get; } = name;
        public string FullPath { get; } = path;
        public long Length { get; } = length;
        public DateTime? CreationTimeUtc { get; } = null;
        public DateTime? LastWriteTimeUtc { get; } = null;
    }

    #endregion

    public bool CanHandle(IDomain domain) => domain.Type == DomainType.Assembly;

    public bool Exists(IPath path) => assembly.GetManifestResourceInfo(path.Value) != null;

    public IFileInfo GetFileInfo(IPath path) => new AFileInfo(
        assembly.GetManifestResourceInfo(path.Value)?.FileName ?? "", path.Value,
        assembly.GetManifestResourceStream(path.Value)?.Length ?? -1);

    public Stream OpenRead(IPath path)
    {
        var stream = assembly.GetManifestResourceStream(path.Value);
        return stream is { CanRead: true }
            ? stream
            : throw new FileNotFoundException(
                $"Resource `{path}` not found or can not be opened for reading.");
    }

    public Stream OpenWrite(IPath path)
    {
        var stream = assembly.GetManifestResourceStream(path.Value);
        return stream is { CanWrite: true }
            ? stream
            : throw new FileNotFoundException(
                $"Resource `{path}` not found or can not be opened for writing.");
    }

    public Stream OpenReadWrite(IPath path)
    {
        var stream = assembly.GetManifestResourceStream(path.Value);
        return stream is { CanRead: true, CanWrite: true }
            ? stream
            : throw new FileNotFoundException(
                $"Resource `{path}` not found or can not be opened for reading and writing.");
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