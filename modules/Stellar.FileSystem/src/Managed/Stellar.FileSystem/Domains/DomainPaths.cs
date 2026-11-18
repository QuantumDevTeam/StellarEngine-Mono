using System.IO.Enumeration;

namespace Stellar.FileSystem.Domains;

/// <summary>
/// Operations on paths inside a domain, in the one form of <see cref="Stellar.Kernel.FileSystem.FileSystemNames"/>.
/// </summary>
public static class DomainPaths
{
    /// <summary>
    /// Joins a directory and a name.
    /// </summary>
    public static string Join(string directory, string name) => directory.Length == 0 ? name : directory + "/" + name;

    /// <summary>
    /// Gets the last segment of a path.
    /// </summary>
    public static string NameOf(string path) => path.Substring(path.LastIndexOf('/') + 1);

    /// <summary>
    /// Gets the directory of a path; an empty string for the root and what lies in it.
    /// </summary>
    public static string ParentOf(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash < 0 ? string.Empty : path.Substring(0, slash);
    }

    /// <summary>
    /// Gets the directories above a path, nearest first, without the root.
    /// </summary>
    public static IEnumerable<string> AncestorsOf(string path)
    {
        for (var slash = path.LastIndexOf('/'); slash > 0; slash = path.LastIndexOf('/', slash - 1))
            yield return path.Substring(0, slash);
    }

    /// <summary>
    /// Gets whether a path lies in a directory: right in it, or at any depth when recursive.
    /// </summary>
    public static bool IsIn(string path, string directory, bool recursive)
    {
        if (path.Length == 0 || path == directory) return false;

        int start;
        if (directory.Length == 0) start = 0;
        else if (path.Length > directory.Length && path[directory.Length] == '/' && path.StartsWith(directory, StringComparison.Ordinal))
            start = directory.Length + 1;
        else return false;

        return recursive || path.IndexOf('/', start) < 0;
    }

    /// <summary>
    /// Gets whether a name matches a pattern of <c>*</c> and <c>?</c>.
    /// </summary>
    public static bool Matches(string pattern, string name, bool ignoreCase = false) =>
        pattern == "*" || FileSystemName.MatchesSimpleExpression(pattern, name, ignoreCase);
}
