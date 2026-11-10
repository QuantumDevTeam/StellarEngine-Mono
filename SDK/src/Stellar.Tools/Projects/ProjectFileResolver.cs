using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobExpressions;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Projects;

/// <summary>
/// A file of a project.
/// </summary>
/// <param name="FullPath">The absolute path.</param>
/// <param name="RelativePath">The path relative to the project directory, with forward slashes.</param>
public sealed record ProjectFile(string FullPath, string RelativePath);

/// <summary>
/// A localization index file.
/// </summary>
/// <param name="File">The file.</param>
/// <param name="Culture">The culture it belongs to.</param>
public sealed record LocalizationFile(ProjectFile File, string Culture);

/// <summary>
/// Everything a project configuration selects on disk.
/// </summary>
public sealed class ResolvedProjectFiles
{
    /// <summary>Gets the assets copied next to the output.</summary>
    public IReadOnlyList<ProjectFile> ExternalAssets { get; init; } = Array.Empty<ProjectFile>();

    /// <summary>Gets the assets embedded into the assembly.</summary>
    public IReadOnlyList<ProjectFile> EmbeddedAssets { get; init; } = Array.Empty<ProjectFile>();

    /// <summary>Gets the sources of compiled resources.</summary>
    public IReadOnlyList<ProjectFile> Resources { get; init; } = Array.Empty<ProjectFile>();

    /// <summary>Gets the localization index files.</summary>
    public IReadOnlyList<LocalizationFile> LocalizationFiles { get; init; } = Array.Empty<LocalizationFile>();

    /// <summary>Gets the default culture, or <c>null</c> if the project has no localization.</summary>
    public string? DefaultCulture { get; init; }

    /// <summary>Gets the supported cultures.</summary>
    public IReadOnlyList<string> SupportedCultures { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Applies the glob patterns of a project configuration to the project directory.
/// </summary>
public static class ProjectFileResolver
{
    /// <summary>
    /// The extension of a localization index file: <c>en.stellar.loc.index</c>.
    /// </summary>
    public const string LocalizationIndexExtension = ".stellar.loc.index";

    private static readonly string[] IgnoredDirectories = ["bin", "obj", ".git", ".vs", ".idea"];

    /// <summary>
    /// Resolves the files of a project.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns>The selected files.</returns>
    /// <exception cref="ToolException">Thrown if the configuration is inconsistent.</exception>
    public static ResolvedProjectFiles Resolve(StellarProject project)
    {
        var allFiles = EnumerateFiles(project.Directory);
        var configuration = project.Configuration;

        var assets = configuration.Assets;
        var embedded = Select(allFiles, assets?.Embedded, assets?.Exclude);
        var embeddedPaths = new HashSet<string>(embedded.Select(file => file.RelativePath), StringComparer.Ordinal);
        var external = Select(allFiles, assets?.Include, assets?.Exclude)
            .Where(file => !embeddedPaths.Contains(file.RelativePath))
            .ToList();

        var resources = configuration.Resources;
        var localizations = configuration.Localizations;
        var cultures = localizations?.Cultures ?? Array.Empty<string>();

        if (localizations?.DefaultCulture is { } defaultCulture && !cultures.Contains(defaultCulture))
            throw new ToolException(
                $"Default culture '{defaultCulture}' is not in the list of supported cultures: " +
                string.Join(", ", cultures));

        return new ResolvedProjectFiles
        {
            ExternalAssets = external,
            EmbeddedAssets = embedded,
            Resources = Select(allFiles, resources?.Include, resources?.Exclude),
            LocalizationFiles = Select(allFiles, localizations?.IndexFiles, null)
                .Select(file => new LocalizationFile(file, CultureOf(file)))
                .ToList(),
            DefaultCulture = localizations?.DefaultCulture,
            SupportedCultures = cultures
        };
    }

    private static string CultureOf(ProjectFile file)
    {
        var name = Path.GetFileName(file.RelativePath);

        return name.EndsWith(LocalizationIndexExtension, StringComparison.OrdinalIgnoreCase)
            ? name.Substring(0, name.Length - LocalizationIndexExtension.Length)
            : Path.GetFileNameWithoutExtension(name);
    }

    private static List<ProjectFile> Select(
        IReadOnlyList<ProjectFile> files, string[]? includePatterns, string[]? excludePatterns)
    {
        if (includePatterns is null || includePatterns.Length == 0)
            return [];

        var include = CompilePatterns(includePatterns);
        var exclude = CompilePatterns(excludePatterns);

        return files
            .Where(file => include.Any(glob => glob.IsMatch(file.RelativePath))
                           && !exclude.Any(glob => glob.IsMatch(file.RelativePath)))
            .ToList();
    }

    private static List<Glob> CompilePatterns(string[]? patterns)
    {
        return (patterns ?? Array.Empty<string>())
            .Where(pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(pattern => new Glob(pattern.Replace('\\', '/')))
            .ToList();
    }

    private static List<ProjectFile> EnumerateFiles(string root)
    {
        var result = new List<ProjectFile>();
        var pending = new Stack<string>();
        pending.Push(root);

        var prefixLength = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1;

        while (pending.Count != 0)
        {
            var directory = pending.Pop();

            foreach (var file in Directory.GetFiles(directory))
                result.Add(new ProjectFile(file, file.Substring(prefixLength).Replace('\\', '/')));

            foreach (var subdirectory in Directory.GetDirectories(directory))
                if (!IgnoredDirectories.Contains(Path.GetFileName(subdirectory), StringComparer.OrdinalIgnoreCase))
                    pending.Push(subdirectory);
        }

        result.Sort((left, right) => string.CompareOrdinal(left.RelativePath, right.RelativePath));
        return result;
    }
}
