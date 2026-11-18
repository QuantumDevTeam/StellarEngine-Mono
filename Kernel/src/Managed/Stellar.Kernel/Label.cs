using System;

namespace Stellar.Kernel;

/// <summary>
/// A name with an identity: an identifier and a human-readable name, such as an event queue or a failure level.
/// </summary>
/// <remarks>
/// <para>A label is a value. Two labels are the same when both their identifiers and their names are: the name is
/// for people, the identifier for the engine. There is no registry of labels and no lookup by name: a label that
/// lost its identifier is a different label.</para>
/// <para><c>default(Label)</c> is <see cref="Empty"/>: no identifier, no name.</para>
/// </remarks>
public readonly struct Label
    : IEquatable<Label>
{
    private readonly string? _name;

    /// <summary>
    /// Makes a label from an identifier and a name.
    /// </summary>
    /// <param name="identifier">The identifier of the label.</param>
    /// <param name="name">The name, not empty.</param>
    /// <exception cref="ArgumentException">Thrown if the name is empty or blank.</exception>
    public Label(Identifier identifier, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A label has a name", nameof(name));

        UID = identifier;
        _name = name;
    }

    /// <summary>
    /// The label of nothing, <c>default(Label)</c>.
    /// </summary>
    public static Label Empty => default;

    /// <summary>
    /// Makes a label with a new identifier.
    /// </summary>
    /// <param name="name">The name, not empty.</param>
    /// <returns>The label.</returns>
    public static Label New(string name) => new(Identifier.New(), name);

    /// <summary>
    /// Gets the identifier of the label.
    /// </summary>
    public Identifier UID { get; }

    /// <summary>
    /// Gets the name of the label; an empty string for <see cref="Empty"/>.
    /// </summary>
    public string Name => _name ?? string.Empty;

    /// <summary>
    /// Gets whether this is <see cref="Empty"/>.
    /// </summary>
    public bool IsEmpty => _name is null;

    /// <inheritdoc/>
    public bool Equals(Label other) => UID == other.UID && string.Equals(_name, other._name, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Label other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => UID.GetHashCode() ^ StringComparer.Ordinal.GetHashCode(Name);

    /// <summary>
    /// Writes the label as its name and identifier.
    /// </summary>
    /// <returns>The text of the label.</returns>
    public override string ToString() => IsEmpty ? string.Empty : $"{Name}#{UID}";

    /// <summary>Compares two labels.</summary>
    public static bool operator ==(Label left, Label right) => left.Equals(right);

    /// <summary>Compares two labels.</summary>
    public static bool operator !=(Label left, Label right) => !left.Equals(right);
}
