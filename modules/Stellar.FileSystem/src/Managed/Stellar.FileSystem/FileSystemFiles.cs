using System.Text;
using Stellar.Kernel.FileSystem;

namespace Stellar.FileSystem;

/// <summary>
/// Whole files at once: bytes and text, read and written through any domain.
/// </summary>
/// <remarks>
/// Text is read in the charset of the content type of the file (<see cref="ContentType.Charset"/>), UTF-8 by
/// default, and a byte order mark wins over both. For large files, or data shared between owners, open a stream or
/// lease a resource instead.
/// </remarks>
public static class FileSystemFiles
{
    private static byte[] ReadToEnd(Stream stream)
    {
        if (stream is MemoryStream memory && memory.TryGetBuffer(out var segment))
            return segment.AsSpan((int)memory.Position).ToArray();

        var buffer = new MemoryStream(stream.CanSeek ? (int)Math.Min(stream.Length, int.MaxValue) : 0);
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static Encoding EncodingOf(IFileSystem fileSystem, FilePath path)
    {
        var charset = fileSystem.ContentTypes.Resolve(path).Charset;
        if (charset is null or "utf-8" or "utf8") return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch (ArgumentException)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
    }

    private static string Decode(ReadOnlySpan<byte> data, Encoding encoding)
    {
        // A byte order mark wins over the charset, as StreamReader does
        if (data.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF])) return Encoding.UTF8.GetString(data[3..]);
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xFE])) return Encoding.Unicode.GetString(data[2..]);
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFE, 0xFF])) return Encoding.BigEndianUnicode.GetString(data[2..]);
        return encoding.GetString(data);
    }

    /// <summary>
    /// Reads the bytes of a file.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The file.</param>
    /// <returns>A copy of its bytes.</returns>
    public static byte[] ReadAllBytes(this IFileSystem fileSystem, FilePath path)
    {
        var domain = fileSystem.GetDomain(path);
        if (domain.TryGetMemory(path.Value, out var memory)) return memory.ToArray();

        using var stream = domain.Open(path.Value,
            FileOpenOptions.Read with { Flags = FileOpenFlags.Sequential });
        return ReadToEnd(stream);
    }

    /// <summary>
    /// Reads the bytes of a file without blocking on a slow source.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The file.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    /// <returns>A copy of its bytes.</returns>
    public static async ValueTask<byte[]> ReadAllBytesAsync(this IFileSystem fileSystem, FilePath path,
        CancellationToken cancellationToken = default)
    {
        var domain = fileSystem.GetDomain(path);
        if (domain.TryGetMemory(path.Value, out var memory)) return memory.ToArray();

        await using var stream = await domain.OpenAsync(path.Value,
                FileOpenOptions.Read with { Flags = FileOpenFlags.Sequential }, cancellationToken)
            .ConfigureAwait(false);
        var buffer = new MemoryStream(stream.CanSeek ? (int)Math.Min(stream.Length, int.MaxValue) : 0);
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    /// <summary>
    /// Reads a file as text.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The file.</param>
    /// <returns>Its text.</returns>
    public static string ReadAllText(this IFileSystem fileSystem, FilePath path)
    {
        var encoding = EncodingOf(fileSystem, path);
        var domain = fileSystem.GetDomain(path);
        if (domain.TryGetMemory(path.Value, out var memory)) return Decode(memory.Span, encoding);

        using var stream = domain.Open(path.Value,
            FileOpenOptions.Read with { Flags = FileOpenFlags.Sequential });
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Writes the bytes of a file, replacing it.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The file.</param>
    /// <param name="data">The bytes.</param>
    /// <param name="atomic">Whether the file is the old one or the new one, never a part of it; needs <see cref="DomainCapabilities.AtomicWrite"/>.</param>
    public static void WriteAllBytes(this IFileSystem fileSystem, FilePath path,
        ReadOnlySpan<byte> data, bool atomic = false)
    {
        using var stream = fileSystem.Open(path, atomic ? FileOpenOptions.Replace : FileOpenOptions.Write);
        stream.Write(data);
    }

    /// <summary>
    /// Writes a file as text, replacing it, in the charset of its content type; UTF-8 with no byte order mark by default.
    /// </summary>
    /// <param name="fileSystem">The file system.</param>
    /// <param name="path">The file.</param>
    /// <param name="text">The text.</param>
    /// <param name="atomic">Whether the file is the old one or the new one, never a part of it; needs <see cref="DomainCapabilities.AtomicWrite"/>.</param>
    public static void WriteAllText(this IFileSystem fileSystem, FilePath path,
        string text, bool atomic = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        fileSystem.WriteAllBytes(path, EncodingOf(fileSystem, path).GetBytes(text), atomic);
    }
}
