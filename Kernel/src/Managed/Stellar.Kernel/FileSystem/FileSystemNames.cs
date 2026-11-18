using System;

namespace Stellar.Kernel.FileSystem;

/// <summary>
/// The rules for the names of the file system: domains and schemes, and the paths inside a domain.
/// </summary>
/// <remarks>
/// <para>A name is lower case ASCII: a letter or a digit, then letters, digits, '.', '_' or '-'. Upper case is
/// folded, so <c>Assets</c> and <c>assets</c> are one name.</para>
/// <para>A path is relative to the root of its domain and uses '/'. Every driver gets it in one form, so a path
/// means the same on every platform and in every domain:</para>
/// <list type="bullet">
/// <item>'\' is read as '/', empty and '.' segments are dropped, a leading '/' means the root;</item>
/// <item>'..' goes up, but never above the root of the domain;</item>
/// <item>a segment has no control characters and none of <c>: * ? " &lt; &gt; |</c>;</item>
/// <item>a segment does not end with '.' or ' ': Windows drops them, so <c>a.</c> would open <c>a</c>.</item>
/// </list>
/// </remarks>
public static class FileSystemNames
{
    /// <summary>
    /// Checks and folds the name of a domain or a scheme.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="parameter">The parameter the name came from, for the exception.</param>
    /// <returns>The name in lower case.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is not a valid name.</exception>
    public static string NormalizeName(string name, string parameter = "name")
    {
        if (name is null) throw new ArgumentNullException(parameter);
        return TryNormalizeName(name, out var normalized)
            ? normalized
            : throw new ArgumentException($"'{name}' is not a valid name: a letter or a digit, then letters, digits, '.', '_' or '-'", parameter);
    }

    /// <summary>
    /// Checks and folds the name of a domain or a scheme.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="normalized">The name in lower case, or an empty string if it is not valid.</param>
    /// <returns><c>true</c> if <paramref name="name"/> is a valid name.</returns>
    public static bool TryNormalizeName(string? name, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrEmpty(name)) return false;

        var lower = true;
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is >= 'A' and <= 'Z') { lower = false; continue; }
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9') continue;
            if (i > 0 && c is '.' or '_' or '-') continue;
            return false;
        }

        normalized = lower ? name : name.ToLowerInvariant();
        return true;
    }

    private static bool TryNormalizePath(string? path, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;
        if (path is null) { error = "no path"; return false; }
        if (path.Length == 0) return true;

        // Most paths are in their form already: check them without allocating
        if (IsNormalized(path, out error))
        {
            if (error.Length != 0) return false;
            normalized = path;
            return true;
        }

        var segments = new string[path.Length];
        var count = 0;
        var start = 0;
        for (var i = 0; i <= path.Length; i++)
        {
            if (i < path.Length && path[i] is not ('/' or '\\')) continue;

            var length = i - start;
            if (length == 0 || (length == 1 && path[start] == '.')) { start = i + 1; continue; }
            if (length == 2 && path[start] == '.' && path[start + 1] == '.')
            {
                if (count == 0) { error = "'..' goes above the root of the domain"; return false; }
                count--;
                start = i + 1;
                continue;
            }

            if (!IsValidSegment(path, start, length, out error)) return false;
            segments[count++] = path.Substring(start, length);
            start = i + 1;
        }

        normalized = string.Join("/", segments, 0, count);
        return true;
    }

    /// <summary>
    /// Brings a path inside a domain to its one form.
    /// </summary>
    /// <param name="path">The path, relative to the root of the domain.</param>
    /// <param name="parameter">The parameter the path came from, for the exception.</param>
    /// <returns>The path in its one form; an empty string for the root.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is not a valid path.</exception>
    public static string NormalizePath(string path, string parameter = "path")
    {
        if (path is null) throw new ArgumentNullException(parameter);
        return TryNormalizePath(path, out var normalized, out var error)
            ? normalized
            : throw new ArgumentException($"'{path}' is not a valid path: {error}", parameter);
    }

    /// <summary>
    /// Brings a path inside a domain to its one form.
    /// </summary>
    /// <param name="path">The path, relative to the root of the domain.</param>
    /// <param name="normalized">The path in its one form, or an empty string if it is not valid.</param>
    /// <returns><c>true</c> if <paramref name="path"/> is a valid path.</returns>
    public static bool TryNormalizePath(string? path, out string normalized) =>
        TryNormalizePath(path, out normalized, out _);

    // true with an empty error: the path is in its form; true with an error: it is invalid; false: rewrite it
    private static bool IsNormalized(string path, out string error)
    {
        error = string.Empty;
        var start = 0;
        for (var i = 0; i <= path.Length; i++)
        {
            if (i < path.Length && path[i] != '/')
            {
                if (path[i] == '\\') return false;
                continue;
            }

            var length = i - start;
            if (length == 0) return false;
            if (path[start] == '.' && (length == 1 || (length == 2 && path[start + 1] == '.'))) return false;
            if (!IsValidSegment(path, start, length, out error)) return true;
            start = i + 1;
        }
        return true;
    }

    private static bool IsValidSegment(string path, int start, int length, out string error)
    {
        error = string.Empty;
        for (var i = start; i < start + length; i++)
        {
            var c = path[i];
            if (c < ' ' || c == '\u007f') { error = "a control character"; return false; }
            if (c is ':' or '*' or '?' or '"' or '<' or '>' or '|') { error = $"'{c}' is not allowed"; return false; }
        }

        var last = path[start + length - 1];
        if (last is '.' or ' ')
        {
            error = $"'{path.Substring(start, length)}' ends with '{last}'";
            return false;
        }
        return true;
    }
}
