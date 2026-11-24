using System;

namespace Stellar.Kernel.FailureSystem;

/// <summary>
/// Where a failure comes from: the engine, a module, the file system, an audio module of a game.
/// </summary>
/// <remarks>
/// <para>An open set, like the schemes of the file system: a module brings its own kinds with
/// <c>new FailureKind("audio")</c>. A name is lower case ASCII: a letter or a digit, then letters, digits, '.', '_'
/// or '-'; upper case is folded.</para>
/// <para><c>default(FailureKind)</c> is <see cref="Unknown"/>.</para>
/// </remarks>
public readonly struct FailureKind
    : IEquatable<FailureKind>
{
    private readonly string? _name;

    /// <summary>
    /// Gets the name of the kind; <c>unknown</c> for <see cref="Unknown"/>.
    /// </summary>
    public string Name => _name ?? "unknown";

    /// <summary>
    /// Makes a kind.
    /// </summary>
    /// <param name="name">The name of the kind.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is not a valid name.</exception>
    public FailureKind(string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));
        if (name.Length == 0) throw new ArgumentException("A kind of failure has a name", nameof(name));

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9') continue;
            if (i > 0 && c is '.' or '_' or '-') continue;
            throw new ArgumentException(
                $"'{name}' is not a valid name: a letter or a digit, then letters, digits, '.', '_' or '-'",
                nameof(name));
        }

        _name = name.ToLowerInvariant();
    }

    #region Defaults

    /// <summary>Nobody said where it comes from.</summary>
    public static FailureKind Unknown => default;

    /// <summary>The engine itself: its runtime, its loops, its lifecycle.</summary>
    public static FailureKind Engine { get; } = new("engine");

    /// <summary>A module: its start, its stop, its work.</summary>
    public static FailureKind Module { get; } = new("module");

    /// <summary>The native core or a native module.</summary>
    public static FailureKind Native { get; } = new("native");

    /// <summary>Stellar.Core.</summary>
    public static FailureKind Core { get; } = new("core");

    /// <summary>The file system and the resources.</summary>
    public static FailureKind FileSystem { get; } = new("filesystem");

    /// <summary>The application: the game itself.</summary>
    public static FailureKind Application { get; } = new("application");

    #endregion

    #region Utility functions

    /// <inheritdoc/>
    public bool Equals(FailureKind other)
    {
        return string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is FailureKind other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Name);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Name;
    }

    #endregion

    #region Operators

    /// <summary>Compares two kinds by name.</summary>
    public static bool operator ==(FailureKind left, FailureKind right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two kinds by name.</summary>
    public static bool operator !=(FailureKind left, FailureKind right)
    {
        return !left.Equals(right);
    }

    #endregion
}
