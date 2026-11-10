using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Stellar.Tools.Resources;

/// <summary>
/// A four-character code. Resource types and chunk tags use them.
/// </summary>
public readonly record struct FourCC
{
    /// <summary>
    /// Creates a code from text, padding it with spaces.
    /// </summary>
    /// <param name="text">At most four ASCII characters.</param>
    public FourCC(string text)
    {
        if (text.Length is 0 or > 4 || text.Any(symbol => symbol > 127))
            throw new ArgumentException("A FourCC is one to four ASCII characters", nameof(text));

        var padded = text.PadRight(4);
        Value = (uint)(padded[0] | (padded[1] << 8) | (padded[2] << 16) | (padded[3] << 24));
    }

    /// <summary>
    /// Creates a code from its numeric value.
    /// </summary>
    public FourCC(uint value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the numeric value, little-endian: the first character is the lowest byte.
    /// </summary>
    public uint Value { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return new string(new[]
        {
            (char)(Value & 0xFF), (char)((Value >> 8) & 0xFF), (char)((Value >> 16) & 0xFF), (char)(Value >> 24)
        });
    }
}

/// <summary>
/// One block of data inside a resource.
/// </summary>
/// <param name="Tag">What the data is.</param>
/// <param name="Data">The data.</param>
public sealed record ResourceChunk(FourCC Tag, byte[] Data);

/// <summary>
/// A compiled resource: a type and a set of chunks.
/// </summary>
/// <param name="Type">The type of the resource. The module that owns the type defines its chunks.</param>
/// <param name="Chunks">The content.</param>
public sealed record CompiledResource(FourCC Type, IReadOnlyList<ResourceChunk> Chunks);

/// <summary>
/// The binary container of compiled resources (<c>.sres</c>).
/// </summary>
/// <remarks>
/// <para>The container knows nothing about what is inside: the engine format of a model or a texture belongs
/// to the module that owns it. The layout is described in Docs/Formats/ResourceContainer.md and mirrored by
/// <c>Resources/ResourceContainer.h</c> of the native Kernel.</para>
/// </remarks>
public static class ResourceContainer
{
    /// <summary>The extension of a container.</summary>
    public const string Extension = ".sres";

    /// <summary>The current format version.</summary>
    public const ushort FormatVersion = 1;

    /// <summary>The alignment of chunk data, in bytes.</summary>
    public const int ChunkAlignment = 16;

    private const uint HeaderSize = 16;
    private const uint TableEntrySize = 24;

    private static readonly FourCC Magic = new("SRES");

    /// <summary>
    /// Writes a resource.
    /// </summary>
    /// <param name="stream">The output.</param>
    /// <param name="resource">The resource.</param>
    public static void Write(Stream stream, CompiledResource resource)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        writer.Write(Magic.Value);
        writer.Write(FormatVersion);
        writer.Write((ushort)0);
        writer.Write(resource.Type.Value);
        writer.Write((uint)resource.Chunks.Count);

        var offset = Align(HeaderSize + TableEntrySize * resource.Chunks.Count);
        var offsets = new long[resource.Chunks.Count];

        for (var index = 0; index < resource.Chunks.Count; index++)
        {
            var chunk = resource.Chunks[index];

            offsets[index] = offset;
            writer.Write(chunk.Tag.Value);
            writer.Write(0u);
            writer.Write((ulong)offset);
            writer.Write((ulong)chunk.Data.LongLength);

            offset = Align(offset + chunk.Data.LongLength);
        }

        var written = HeaderSize + TableEntrySize * resource.Chunks.Count;

        for (var index = 0; index < resource.Chunks.Count; index++)
        {
            writer.Write(new byte[offsets[index] - written]);
            writer.Write(resource.Chunks[index].Data);
            written = offsets[index] + resource.Chunks[index].Data.LongLength;
        }
    }

    /// <summary>
    /// Reads a resource.
    /// </summary>
    /// <param name="stream">The input. It must be seekable.</param>
    /// <returns>The resource.</returns>
    /// <exception cref="InvalidDataException">Thrown if the data is not a valid container.</exception>
    public static CompiledResource Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);

        if (reader.ReadUInt32() != Magic.Value)
            throw new InvalidDataException("Not a Stellar resource container");

        var version = reader.ReadUInt16();
        if (version != FormatVersion)
            throw new InvalidDataException($"Unsupported container version {version}");

        reader.ReadUInt16();

        var type = new FourCC(reader.ReadUInt32());
        var count = reader.ReadUInt32();

        var length = (ulong)stream.Length;

        if (HeaderSize + (ulong)count * TableEntrySize > length)
            throw new InvalidDataException("The chunk table is outside the container");

        var table = new List<(FourCC Tag, ulong Offset, ulong Size)>();
        for (var index = 0u; index < count; index++)
        {
            var tag = new FourCC(reader.ReadUInt32());
            reader.ReadUInt32();
            table.Add((tag, reader.ReadUInt64(), reader.ReadUInt64()));
        }

        var chunks = new List<ResourceChunk>(table.Count);
        foreach (var (tag, offset, size) in table)
        {
            // Compared this way round so that a forged offset or size cannot overflow past the check
            if (size > length || offset > length - size || size > int.MaxValue)
                throw new InvalidDataException($"Chunk {tag} is outside the container");

            stream.Position = (long)offset;
            chunks.Add(new ResourceChunk(tag, reader.ReadBytes((int)size)));
        }

        return new CompiledResource(type, chunks);
    }

    private static long Align(long value)
    {
        return (value + ChunkAlignment - 1) / ChunkAlignment * ChunkAlignment;
    }
}
