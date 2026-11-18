using System;

namespace Stellar.Kernel.Profiling;

/// <summary>
/// Where a marked span of work belongs: an open set of names, as the kinds of failures are. A module adds its own:
/// <c>new ProfilerCategory("audio")</c>.
/// </summary>
/// <remarks>Names are compared in lower case: a letter or a digit, then letters, digits, '.', '_' or '-'.</remarks>
public readonly struct ProfilerCategory
    : IEquatable<ProfilerCategory>
{
    private readonly string? _name;

    /// <summary>
    /// Makes a category.
    /// </summary>
    /// <param name="name">Its name.</param>
    /// <exception cref="ArgumentException">Thrown for a name that is not valid.</exception>
    public ProfilerCategory(string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));
        if (name.Length == 0) throw new ArgumentException("A category has a name", nameof(name));

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9') continue;
            if (i > 0 && c is '.' or '_' or '-') continue;
            throw new ArgumentException($"'{name}' is not a valid name: a letter or a digit, then letters, digits, '.', '_' or '-'", nameof(name));
        }

        _name = name.ToLowerInvariant();
    }

    /// <summary>Gets the category of what says nothing of its own.</summary>
    public static ProfilerCategory Other => default;

    /// <summary>Gets the category of the frames and phases of the main loops.</summary>
    public static ProfilerCategory Loop { get; } = new("loop");

    /// <summary>Gets the category of the update phases of participants.</summary>
    public static ProfilerCategory Update { get; } = new("update");

    /// <summary>Gets the category of the render phases of participants.</summary>
    public static ProfilerCategory Render { get; } = new("render");

    /// <summary>Gets the category of the physics and other fixed steps.</summary>
    public static ProfilerCategory Physics { get; } = new("physics");

    /// <summary>Gets the category of the jobs and of work posted between threads.</summary>
    public static ProfilerCategory Jobs { get; } = new("jobs");

    /// <summary>Gets the category of memory: allocations, collections of the garbage collector.</summary>
    public static ProfilerCategory Memory { get; } = new("memory");

    /// <summary>Gets the category of files and resources.</summary>
    public static ProfilerCategory FileSystem { get; } = new("filesystem");

    /// <summary>Gets the category of the start and the stop of modules.</summary>
    public static ProfilerCategory Modules { get; } = new("modules");

    /// <summary>Gets the category of the code of the application.</summary>
    public static ProfilerCategory Application { get; } = new("application");

    /// <summary>
    /// Gets the name, in lower case.
    /// </summary>
    public string Name => _name ?? "other";

    /// <inheritdoc/>
    public bool Equals(ProfilerCategory other)
    {
        return string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ProfilerCategory other && Equals(other);
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

    /// <summary>Compares two categories.</summary>
    public static bool operator ==(ProfilerCategory left, ProfilerCategory right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two categories.</summary>
    public static bool operator !=(ProfilerCategory left, ProfilerCategory right)
    {
        return !left.Equals(right);
    }
}
