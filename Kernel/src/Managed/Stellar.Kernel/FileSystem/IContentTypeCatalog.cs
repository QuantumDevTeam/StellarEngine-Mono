using Stellar.Kernel.Data.Catalog;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The content types of a file system, by extension: the extension with its dot, such as <c>.png</c>, in any case.
/// </summary>
/// <remarks>
/// Drivers fill <see cref="FileEntry.Content"/> from here, unless their source tells it (a network share with a
/// content type of its own). Modules add the formats they read. The catalog refuses what is not an extension and the
/// unknown content type.
/// </remarks>
public interface IContentTypeCatalog
    : ICatalog<string, ContentType>
{
    /// <summary>
    /// Gets the content type of a file by its extension.
    /// </summary>
    /// <param name="path">The file.</param>
    /// <returns>The content type, or <see cref="ContentType.OctetStream"/> for an unknown extension.</returns>
    ContentType Resolve(FilePath path);
}
