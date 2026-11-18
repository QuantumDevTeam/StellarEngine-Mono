using System;
using System.Security.Cryptography;

namespace Stellar.Kernel;

/// <summary>
/// Makes the values of new identifiers: random bytes from the random source of the system, asked for many identifiers
/// at once and spent one by one, per thread. As random as <see cref="Guid.NewGuid"/>, a few times cheaper.
/// </summary>
internal static class IdentifierSource
{
    // Identifiers per call to the random source of the system
    private const int Batch = 128;

    [ThreadStatic] private static byte[]? _buffer;
    [ThreadStatic] private static int _offset;

#if !NET
    [ThreadStatic] private static RandomNumberGenerator? _random;
#endif

    /// <summary>
    /// Gets the next value: a version 4 UUID.
    /// </summary>
    public static Guid Next()
    {
        var buffer = _buffer;
        var offset = _offset;
        if (buffer is null || offset >= buffer.Length)
        {
            buffer = Refill();
            offset = 0;
        }

        _offset = offset + Identifier.Size;

        // The version (4) and the variant (RFC 4122), as Guid.NewGuid sets them
        buffer[offset + 7] = (byte)((buffer[offset + 7] & 0x0F) | 0x40);
        buffer[offset + 8] = (byte)((buffer[offset + 8] & 0x3F) | 0x80);

#if NET
        return new Guid(buffer.AsSpan(offset, Identifier.Size));
#else
        return new Guid(BitConverter.ToInt32(buffer, offset), BitConverter.ToInt16(buffer, offset + 4),
            BitConverter.ToInt16(buffer, offset + 6), buffer[offset + 8], buffer[offset + 9], buffer[offset + 10],
            buffer[offset + 11], buffer[offset + 12], buffer[offset + 13], buffer[offset + 14], buffer[offset + 15]);
#endif
    }

    private static byte[] Refill()
    {
        var buffer = _buffer ??= new byte[Batch * Identifier.Size];
#if NET
        RandomNumberGenerator.Fill(buffer);
#else
        (_random ??= RandomNumberGenerator.Create()).GetBytes(buffer);
#endif
        return buffer;
    }
}
