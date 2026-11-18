using System;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The address of a file or a directory: a domain and a path inside it, written <c>domain://path</c>.
/// </summary>
/// <remarks>
/// <para>A path is a value, not a quant: it allocates nothing beyond its two strings, compares by value and is
/// checked once, when it is made. Every path is in the one form of <see cref="FileSystemNames"/>, so two paths to
/// the same file are equal, and no path leaves its domain.</para>
/// <para>The domain is a name mounted in an <see cref="IFileSystem"/>, never a location of the machine:
/// <c>assets://textures/hero.png</c>, not <c>C:\Game\textures\hero.png</c>. Where the domain lies is the business of
/// the driver that mounted it.</para>
/// <para>Paths compare by ordinal: whether <c>a.png</c> and <c>A.png</c> are one file is up to the domain, and the
/// engine never relies on it.</para>
/// <para><c>default(FilePath)</c> is the empty path (<see cref="IsEmpty"/>); it names nothing.</para>
/// </remarks>
public readonly struct FilePath
    : IEquatable<FilePath>, IComparable<FilePath>
{
    /// <summary>
    /// What separates the domain from the path in the text of a path.
    /// </summary>
    public const string DomainSeparator = "://";

    // Fields, not the C# 14 'field' keyword: the SDK of global.json is .NET 9 (C# 13)
    private readonly string? _domain;
    private readonly string? _value;

    /// <summary>
    /// Gets the name of the domain.
    /// </summary>
    public string Domain => _domain ?? string.Empty;

    /// <summary>
    /// Gets the path inside the domain, in its one form; an empty string for the root.
    /// </summary>
    public string Value => _value ?? string.Empty;

    /// <summary>
    /// Gets whether this is the empty path, <c>default(FilePath)</c>.
    /// </summary>
    public bool IsEmpty => _domain is null;

    /// <summary>
    /// Gets whether this is the root of its domain.
    /// </summary>
    public bool IsRoot => _domain is not null && Value.Length == 0;

    /// <summary>
    /// Gets the last segment: the name of the file or the directory; an empty string for the root.
    /// </summary>
    public string Name
    {
        get
        {
            var value = Value;
            return value.Substring(value.LastIndexOf('/') + 1);
        }
    }

    /// <summary>
    /// Gets the extension of <see cref="Name"/> with its dot, such as <c>.png</c>; an empty string if there is none.
    /// </summary>
    public string Extension
    {
        get
        {
            var name = Name;
            var dot = name.LastIndexOf('.');
            return dot < 0 ? string.Empty : name.Substring(dot);
        }
    }

    /// <summary>
    /// Gets <see cref="Name"/> without its <see cref="Extension"/>.
    /// </summary>
    public string NameWithoutExtension
    {
        get
        {
            var name = Name;
            var dot = name.LastIndexOf('.');
            return dot < 0 ? name : name.Substring(0, dot);
        }
    }

    /// <summary>
    /// Gets the directory this path lies in.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown for the root and the empty path, which have none.</exception>
    public FilePath Parent
    {
        get
        {
            if (_domain is null || Value.Length == 0) throw new InvalidOperationException($"'{this}' has no parent");
            var slash = Value.LastIndexOf('/');
            return new FilePath(_domain, slash < 0 ? string.Empty : Value.Substring(0, slash));
        }
    }

    private FilePath(string domain, string value)
    {
        _domain = domain;
        _value = value;
    }

    /// <summary>
    /// Makes the root of a domain.
    /// </summary>
    /// <param name="domain">The name of the domain.</param>
    /// <returns>The root of <paramref name="domain"/>.</returns>
    public static FilePath Root(string domain) =>
        new(FileSystemNames.NormalizeName(domain, nameof(domain)), string.Empty);

    /// <summary>
    /// Makes a path from a domain and a path inside it.
    /// </summary>
    /// <param name="domain">The name of the domain.</param>
    /// <param name="path">The path inside the domain, in any form <see cref="FileSystemNames"/> accepts.</param>
    /// <returns>The path.</returns>
    /// <exception cref="ArgumentException">Thrown if the domain or the path is not valid.</exception>
    public static FilePath Create(string domain, string path) =>
        // ReSharper disable once RedundantArgumentDefaultValue
        new(FileSystemNames.NormalizeName(domain, nameof(domain)), FileSystemNames.NormalizePath(path, nameof(path)));

    /// <summary>
    /// Reads a path written <c>domain://path</c>.
    /// </summary>
    /// <param name="text">The text of the path.</param>
    /// <returns>The path.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="text"/> is not a valid path.</exception>
    public static FilePath Parse(string text) =>
        TryParse(text, out var path)
            ? path
            : throw new FormatException($"'{text}' is not a path: expected domain://path");

    /// <summary>
    /// Reads a path written <c>domain://path</c>.
    /// </summary>
    /// <param name="text">The text of the path.</param>
    /// <param name="path">The path, or the empty path if <paramref name="text"/> is not valid.</param>
    /// <returns><c>true</c> if <paramref name="text"/> is a valid path.</returns>
    public static bool TryParse(string? text, out FilePath path)
    {
        path = default;
        if (text is null) return false;

        var separator = text.IndexOf(DomainSeparator, StringComparison.Ordinal);
        if (separator <= 0) return false;

        if (!FileSystemNames.TryNormalizeName(text.Substring(0, separator), out var domain))
            return false;
        if (!FileSystemNames.TryNormalizePath(text.Substring(separator + DomainSeparator.Length), out var value))
            return false;

        path = new FilePath(domain, value);
        return true;
    }

    /// <summary>
    /// Goes from this directory by a relative path.
    /// </summary>
    /// <param name="relative">The way to go: '..' goes up, a leading '/' starts from the root of the domain.</param>
    /// <returns>The path reached.</returns>
    /// <exception cref="ArgumentException">Thrown if the way is not valid or goes above the root.</exception>
    /// <exception cref="InvalidOperationException">Thrown for the empty path.</exception>
    public FilePath Combine(string relative)
    {
        if (_domain is null) throw new InvalidOperationException("The empty path cannot be combined");
        if (relative is null) throw new ArgumentNullException(nameof(relative));
        if (relative.Length == 0) return this;

        var rooted = relative[0] is '/' or '\\';
        var joined = rooted || Value.Length == 0 ? relative : Value + "/" + relative;
        return new FilePath(_domain, FileSystemNames.NormalizePath(joined, nameof(relative)));
    }

    /// <summary>
    /// Gets the same path in another domain: a layer of an overlay, a copy, a cache.
    /// </summary>
    /// <param name="domain">The name of the other domain.</param>
    /// <returns>The path inside <paramref name="domain"/>.</returns>
    public FilePath WithDomain(string domain) =>
        new(FileSystemNames.NormalizeName(domain, nameof(domain)), Value);

    /// <summary>
    /// Gets whether this path lies inside a directory, at any depth.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <returns><c>true</c> if this path is inside <paramref name="directory"/>, but not <paramref name="directory"/> itself.</returns>
    public bool IsInside(FilePath directory)
    {
        if (_domain is null || directory._domain is null) return false;
        if (!string.Equals(_domain, directory._domain, StringComparison.Ordinal)) return false;
        if (directory.Value.Length == 0) return Value.Length > 0;

        return Value.Length > directory.Value.Length
               && Value[directory.Value.Length] == '/'
               && Value.StartsWith(directory.Value, StringComparison.Ordinal);
    }

    #region Utility functions

    /// <inheritdoc/>
    public bool Equals(FilePath other) =>
        string.Equals(_domain, other._domain, StringComparison.Ordinal) &&
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is FilePath other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _domain is null
            ? 0
            : HashCode.Combine(StringComparer.Ordinal.GetHashCode(_domain), StringComparer.Ordinal.GetHashCode(Value));

    /// <inheritdoc/>
    public int CompareTo(FilePath other)
    {
        var domain = string.CompareOrdinal(_domain, other._domain);
        return domain != 0 ? domain : string.CompareOrdinal(Value, other.Value);
    }

    /// <summary>
    /// Writes the path as <c>domain://path</c>; the empty path as an empty string.
    /// </summary>
    /// <returns>The text of the path, which <see cref="Parse"/> reads back.</returns>
    public override string ToString() => _domain is null ? string.Empty : _domain + DomainSeparator + Value;

    #endregion

    #region Operators

    /// <summary>Compares two paths by value.</summary>
    public static bool operator ==(FilePath left, FilePath right) => left.Equals(right);

    /// <summary>Compares two paths by value.</summary>
    public static bool operator !=(FilePath left, FilePath right) => !left.Equals(right);

    #endregion
}
