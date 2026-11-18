using System;
using System.Runtime.InteropServices;

namespace Stellar.Kernel;

/// <summary>
/// The identity of an object of the engine: a value of 128 bits, unique without any registry.
/// </summary>
/// <remarks>
/// <para>An identifier is a value, nothing more: it is made, compared, written and read, and it never knows where it
/// is used. Registries hold objects by their identifiers; adding or removing one changes nothing here.</para>
/// <para>What it is made of is its own business: code of the engine works with identifiers, never with what is
/// inside. The layout is blittable, 16 bytes, the same as <c>StellarUid</c> of the native core, so an identifier
/// crosses into native code as it is.</para>
/// <para>The text form is the 36 characters <c>xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx</c> in lower case, the same in the
/// native core.</para>
/// <para><c>default(Identifier)</c> is <see cref="Empty"/>: it identifies nothing.</para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Identifier
    : IEquatable<Identifier>, IComparable<Identifier>
{
    /// <summary>
    /// The length of an identifier in bytes.
    /// </summary>
    public const int Size = 16;

    private readonly Guid _value;

    private Identifier(Guid value)
    {
        _value = value;
    }

    /// <summary>
    /// The identifier of nothing, <c>default(Identifier)</c>.
    /// </summary>
    public static Identifier Empty => default;

    /// <summary>
    /// Gets whether this is <see cref="Empty"/>.
    /// </summary>
    public bool IsEmpty => _value == Guid.Empty;

    /// <summary>
    /// Makes a new identifier, different from every other: 122 random bits from the random source of the system, as a
    /// version 4 UUID.
    /// </summary>
    /// <returns>The identifier.</returns>
    public static Identifier New() => new(IdentifierSource.Next());

    /// <summary>
    /// Reads an identifier from its text form.
    /// </summary>
    /// <param name="text">The 36 characters of an identifier, in any case.</param>
    /// <returns>The identifier.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="text"/> is not an identifier.</exception>
    public static Identifier Parse(string text) =>
        TryParse(text, out var identifier) ? identifier : throw new FormatException($"'{text}' is not an identifier");

    /// <summary>
    /// Reads an identifier from its text form.
    /// </summary>
    /// <param name="text">The 36 characters of an identifier, in any case.</param>
    /// <param name="identifier">The identifier, or <see cref="Empty"/>.</param>
    /// <returns><c>true</c> if <paramref name="text"/> is an identifier.</returns>
    public static bool TryParse(string? text, out Identifier identifier)
    {
        identifier = default;
        if (text is null || text.Length != 36 || !Guid.TryParseExact(text, "D", out var value)) return false;

        identifier = new Identifier(value);
        return true;
    }

    /// <summary>
    /// Makes an identifier from its 16 bytes, as <see cref="ToBytes"/> gives them.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The identifier.</returns>
    /// <exception cref="ArgumentException">Thrown if there are not 16 bytes.</exception>
    public static Identifier FromBytes(byte[] bytes)
    {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != Size) throw new ArgumentException($"An identifier is {Size} bytes, not {bytes.Length}", nameof(bytes));
        return new Identifier(new Guid(bytes));
    }

    /// <summary>
    /// Gets the 16 bytes of the identifier, in the order of the native core.
    /// </summary>
    /// <returns>A new array of 16 bytes.</returns>
    public byte[] ToBytes() => _value.ToByteArray();

#if NET
    /// <summary>
    /// Makes an identifier from its 16 bytes, as <see cref="TryWriteBytes"/> writes them.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The identifier.</returns>
    /// <exception cref="ArgumentException">Thrown if there are not 16 bytes.</exception>
    public static Identifier FromBytes(ReadOnlySpan<byte> bytes) =>
        bytes.Length == Size
            ? new Identifier(new Guid(bytes))
            : throw new ArgumentException($"An identifier is {Size} bytes, not {bytes.Length}", nameof(bytes));

    /// <summary>
    /// Writes the 16 bytes of the identifier, in the order of the native core.
    /// </summary>
    /// <param name="destination">Where to write them.</param>
    /// <returns><c>true</c> if <paramref name="destination"/> has room for 16 bytes.</returns>
    public bool TryWriteBytes(Span<byte> destination) => _value.TryWriteBytes(destination);
#endif

    /// <inheritdoc/>
    public bool Equals(Identifier other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Identifier other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <inheritdoc/>
    public int CompareTo(Identifier other) => _value.CompareTo(other._value);

    /// <summary>
    /// Writes the identifier in its text form, which <see cref="Parse"/> reads back.
    /// </summary>
    /// <returns>The 36 characters of the identifier, in lower case.</returns>
    public override string ToString() => _value.ToString("D");

    /// <summary>Compares two identifiers.</summary>
    public static bool operator ==(Identifier left, Identifier right) => left.Equals(right);

    /// <summary>Compares two identifiers.</summary>
    public static bool operator !=(Identifier left, Identifier right) => !left.Equals(right);
}
