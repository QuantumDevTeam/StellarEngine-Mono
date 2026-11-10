using Stellar.Kernel;
using Stellar.Kernel.FileSystem;
using Stellar.Core.Quantization;

namespace Stellar.FileSystem;

/// <summary>
/// Quantum File with a content
/// </summary>
/// <param name="pathtion">File location</param>
/// <param name="type">File type</param>
/// <param name="identifier">An unique identifier</param>
public class File(Path path, FileType type, IIdentifier? identifier = null)
    : MetaQuant(identifier), IFile
{
    /// <summary>
    /// File location
    /// </summary>
    public IPath Path { get; } = path;

    /// <summary>
    /// File type
    /// </summary>
    public IFileType Type { get; } = type;

    private bool Equals(File other)
    {
        return other.Path == Path;
    }

    public bool Equals(IFile? obj)
    {
        return ReferenceEquals(this, obj) || obj is File other && Equals(other);
    }
}