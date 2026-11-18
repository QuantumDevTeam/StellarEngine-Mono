using System;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// How the bytes of a file are to be read: as bytes or as text.
/// </summary>
public enum ContentKind : byte
{
    /// <summary>Bytes: images, sounds, meshes, packed data.</summary>
    Binary,

    /// <summary>Text in a <see cref="ContentType.Charset"/>: JSON, shaders, scripts, localization.</summary>
    Text
}

/// <summary>
/// What a file holds: a media type, such as <c>image/png</c>, and whether it is text or bytes.
/// </summary>
/// <remarks>
/// <para>The content type says how to read a file, never what the file becomes: one JSON file may give the asset
/// system a material, a level and a dozen prefabs. Assets are decided above the file system.</para>
/// <para><c>default(ContentType)</c> is the unknown content (<see cref="IsUnknown"/>), read as
/// <see cref="OctetStream"/>.</para>
/// </remarks>
public readonly struct ContentType
    : IEquatable<ContentType>
{
    private readonly string? _mediaType;

    /// <summary>
    /// Gets the media type; <c>application/octet-stream</c> when unknown.
    /// </summary>
    public string MediaType => _mediaType ?? "application/octet-stream";

    /// <summary>
    /// Gets whether the content is text or bytes.
    /// </summary>
    public ContentKind Kind { get; }

    /// <summary>
    /// Gets the encoding of text, such as <c>utf-8</c>; <c>null</c> for bytes or when unknown.
    /// </summary>
    public string? Charset { get; }

    /// <summary>
    /// Gets whether this is the unknown content, <c>default(ContentType)</c>.
    /// </summary>
    public bool IsUnknown => _mediaType is null;

    /// <summary>
    /// Gets whether the content is text.
    /// </summary>
    public bool IsText => Kind == ContentKind.Text;

    /// <summary>
    /// Makes a content type.
    /// </summary>
    /// <param name="mediaType">The media type, <c>type/subtype</c>, such as <c>image/png</c>.</param>
    /// <param name="kind">Whether the content is text or bytes.</param>
    /// <param name="charset">The encoding of text, such as <c>utf-8</c>; <c>null</c> for bytes or when unknown.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="mediaType"/> is not <c>type/subtype</c>.</exception>
    public ContentType(string mediaType, ContentKind kind, string? charset = null)
    {
        if (mediaType is null) throw new ArgumentNullException(nameof(mediaType));

        var slash = mediaType.IndexOf('/');
        if (slash <= 0
            || slash == mediaType.Length - 1
            || mediaType.IndexOf('/', slash + 1) >= 0
            || mediaType.IndexOf(' ') >= 0)
            throw new ArgumentException($"'{mediaType}' is not a media type: expected type/subtype", nameof(mediaType));

        _mediaType = mediaType.ToLowerInvariant();
        Kind = kind;
        Charset = charset?.ToLowerInvariant();
    }

    #region Defaults

    /// <summary>Bytes of no known type.</summary>
    public static ContentType OctetStream { get; } = new("application/octet-stream", ContentKind.Binary);

    /// <summary>Plain text in UTF-8.</summary>
    public static ContentType PlainText { get; } = new("text/plain", ContentKind.Text, "utf-8");

    #endregion

    #region Utility functions

    /// <inheritdoc/>
    public bool Equals(ContentType other) =>
        string.Equals(_mediaType, other._mediaType, StringComparison.Ordinal)
        && Kind == other.Kind
        && string.Equals(Charset, other.Charset, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is ContentType other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_mediaType, Kind, Charset);

    /// <summary>
    /// Writes the content type as a media type with its charset, such as <c>text/plain; charset=utf-8</c>.
    /// </summary>
    /// <returns>The text of the content type.</returns>
    public override string ToString() => Charset is null ? MediaType : $"{MediaType}; charset={Charset}";

    #endregion

    #region Operators

    /// <summary>Compares two content types by value.</summary>
    public static bool operator ==(ContentType left, ContentType right) => left.Equals(right);

    /// <summary>Compares two content types by value.</summary>
    public static bool operator !=(ContentType left, ContentType right) => !left.Equals(right);

    #endregion
}
