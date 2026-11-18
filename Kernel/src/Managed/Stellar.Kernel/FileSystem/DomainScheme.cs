using System;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The kind of a domain: which driver mounts it, such as <c>dir</c>, <c>asm</c> or <c>zip</c>.
/// </summary>
/// <remarks>
/// <para>A scheme is an open set: a driver brings its own (<see cref="IFileDriver.Scheme"/>), so a module, a tool
/// or a game adds archives, network shares or a file system of its own world without touching the engine.</para>
/// <para>The names follow <see cref="FileSystemNames"/>. The well-known ones below are conventions, not a list:
/// a file system has only the drivers registered in it.</para>
/// </remarks>
public readonly struct DomainScheme
    : IEquatable<DomainScheme>
{
    /// <summary>
    /// Gets the name of the scheme; an empty string for <c>default(DomainScheme)</c>.
    /// </summary>
    public string Name => _name ?? string.Empty;

    // A field, not the C# 14 'field' keyword: the SDK of global.json is .NET 9 (C# 13)
    private readonly string? _name;

    /// <summary>
    /// Makes a scheme.
    /// </summary>
    /// <param name="name">The name of the scheme.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is not a valid name.</exception>
    public DomainScheme(string name)
    {
        // ReSharper disable once RedundantArgumentDefaultValue
        _name = FileSystemNames.NormalizeName(name, nameof(name));
    }

    #region Defaults

    /// <summary>A directory of the machine.</summary>
    public static DomainScheme Directory { get; } = new("dir");

    /// <summary>The resources embedded in an assembly.</summary>
    public static DomainScheme Assembly { get; } = new("asm");

    /// <summary>Files held in memory: tests, generated data.</summary>
    public static DomainScheme Memory { get; } = new("mem");

    /// <summary>Several domains in layers, the first that has a file wins: mods over DLC over the game.</summary>
    public static DomainScheme Overlay { get; } = new("overlay");

    #endregion

    #region Utility functions

    /// <inheritdoc/>
    public bool Equals(DomainScheme other) => string.Equals(Name, other.Name, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DomainScheme other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

    /// <inheritdoc/>
    public override string ToString() => Name;

    #endregion

    #region Operators

    /// <summary>Compares two schemes by name.</summary>
    public static bool operator ==(DomainScheme left, DomainScheme right) => left.Equals(right);

    /// <summary>Compares two schemes by name.</summary>
    public static bool operator !=(DomainScheme left, DomainScheme right) => !left.Equals(right);

    #endregion
}
