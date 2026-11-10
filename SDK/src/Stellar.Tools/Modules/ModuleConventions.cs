using System;
using System.IO;
using System.Linq;
using Stellar.Kernel.Modules;

namespace Stellar.Tools.Modules;

/// <summary>
/// The defaults a module gets for what its descriptor leaves out. A module folder mirrors the core:
/// <code>
/// &lt;Id&gt;/
/// ├── &lt;Id&gt;.stellar.module
/// ├── src/Managed/&lt;Id&gt;/&lt;Id&gt;.csproj
/// ├── src/Native/...            native projects
/// ├── src/NativeInterop/...     wrappers of the native part
/// └── tests/...
/// </code>
/// </summary>
public static class ModuleConventions
{
    /// <summary>
    /// The folders native projects are taken from, in build order.
    /// </summary>
    public static readonly string[] NativeDirectories =
        { Path.Combine("src", "Native"), Path.Combine("src", "NativeInterop") };

    /// <summary>
    /// The folder test projects are taken from.
    /// </summary>
    public const string TestsDirectory = "tests";

    /// <summary>
    /// Gets the file name of the descriptor of a module.
    /// </summary>
    public static string DescriptorFileName(string id)
    {
        return id + ModuleDescriptor.FileExtension;
    }

    /// <summary>
    /// Finds the managed project of a module by the convention.
    /// </summary>
    /// <param name="directory">The module folder.</param>
    /// <param name="id">The module identifier.</param>
    /// <returns>The path relative to the folder, or <c>null</c> if the module has no managed project.</returns>
    public static string? FindManagedProject(string directory, string id)
    {
        var candidates = new[]
        {
            Path.Combine("src", "Managed", id, id + ".csproj"),
            Path.Combine("src", id, id + ".csproj"),
            id + ".csproj"
        };

        return candidates.FirstOrDefault(candidate => File.Exists(Path.Combine(directory, candidate)));
    }

    /// <summary>
    /// Finds the native projects of a module by the convention.
    /// </summary>
    /// <param name="directory">The module folder.</param>
    /// <returns>The paths relative to the folder, in build order.</returns>
    public static string[] FindNativeProjects(string directory)
    {
        return NativeDirectories
            .SelectMany(folder => FindProjects(directory, folder, "*.vcxproj"))
            .ToArray();
    }

    /// <summary>
    /// Finds the test projects of a module by the convention.
    /// </summary>
    /// <param name="directory">The module folder.</param>
    /// <returns>The paths relative to the folder.</returns>
    public static string[] FindTestProjects(string directory)
    {
        return FindProjects(directory, TestsDirectory, "*.csproj")
            .Concat(FindProjects(directory, TestsDirectory, "*.vcxproj"))
            .ToArray();
    }

    private static string[] FindProjects(string directory, string folder, string pattern)
    {
        var root = Path.Combine(directory, folder);
        if (!Directory.Exists(root))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .Select(path =>
                path.Substring(directory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsBuildOutput(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }
}
