using Stellar.Core.Data.Catalog;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem;

/// <summary>
/// The content types of a file system, by extension; the extensions compare in any case.
/// </summary>
public sealed class ContentTypeCatalog()
    : Catalog<string, ContentType>(StringComparer.OrdinalIgnoreCase), IContentTypeCatalog
{
    /// <summary>
    /// Makes a catalog with the formats every engine reads: text, data, images, sounds, models, fonts, shaders.
    /// </summary>
    /// <returns>The catalog.</returns>
    public static ContentTypeCatalog CreateDefault()
    {
        var catalog = new ContentTypeCatalog();

        // text
        foreach (var (extension, mediaType) in new[]
                 {
                     // application/*
                     (".json", "application/json"), (".xml", "application/xml"), (".yaml", "application/yaml"),
                     (".yml", "application/yaml"), (".toml", "application/toml"),
                     // plain text
                     (".txt", "text/plain"), (".ini", "text/plain"), (".cfg", "text/plain"),
                     (".md", "text/markdown"), (".csv", "text/csv"),
                     // models
                     (".gltf", "model/gltf+json"), (".obj", "model/obj"),
                     // shaders
                     (".shader", "text/x-shader"),
                     (".hlsl", "text/x-hlsl"), (".wgsl", "text/wgsl"), (".glsl", "text/x-glsl"),
                     // languages
                     (".lua", "text/x-lua"), (".cs", "text/x-csharp"), (".js", "text/javascript"),
                     (".html", "text/html"), (".css", "text/css"),
                     // images
                     (".svg", "image/svg+xml")
                 })
            catalog.Set(extension, new ContentType(mediaType, ContentKind.Text, "utf-8"));

        // binary
        foreach (var (extension, mediaType) in new[]
                 {
                     // images
                     (".png", "image/png"), (".jpg", "image/jpeg"), (".jpeg", "image/jpeg"), (".bmp", "image/bmp"),
                     (".tga", "image/x-tga"), (".dds", "image/vnd-ms.dds"), (".ktx2", "image/ktx2"),
                     (".hdr", "image/vnd.radiance"), (".exr", "image/x-exr"), (".webp", "image/webp"),
                     // audio
                     (".wav", "audio/wav"), (".ogg", "audio/ogg"), (".mp3", "audio/mpeg"), (".flac", "audio/flac"),
                     // models
                     (".glb", "model/gltf-binary"), (".fbx", "model/x-fbx"),
                     // fonts
                     (".ttf", "font/ttf"), (".otf", "font/otf"),
                     // shaders
                     (".spv", "application/x-spirv"),
                     // other
                     (".bin", "application/octet-stream"),
                     (".zip", "application/zip"),
                     (".dll", "application/x-msdownload"),
                 })
            catalog.Set(extension, new ContentType(mediaType, ContentKind.Binary));

        return catalog;
    }

    /// <inheritdoc/>
    public ContentType Resolve(FilePath path)
    {
        var extension = path.Extension;
        return extension.Length > 0 && TryGet(extension, out var contentType)
            ? contentType
            : ContentType.OctetStream;
    }

    /// <inheritdoc/>
    protected override string NormalizeKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length < 2 || key[0] != '.' || key.IndexOfAny(['/', '\\', ' ']) >= 0)
            throw new ArgumentException($"'{key}' is not an extension: " +
                                        $"expected a dot and a name, such as .png", nameof(key));
        return key;
    }

    /// <inheritdoc/>
    protected override void Validate(string key, ContentType value)
    {
        if (value.IsUnknown)
            throw new ArgumentException($"The unknown content type cannot be set for '{key}'", nameof(value));
    }
}
