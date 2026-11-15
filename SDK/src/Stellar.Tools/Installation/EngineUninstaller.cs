using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Stellar.Kernel.Configuration.Manifest;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Installation;

/// <summary>
/// What <see cref="EngineUninstaller"/> removes beyond the installation itself.
/// </summary>
public sealed class UninstallOptions
{
    /// <summary>
    /// Gets or sets the registry to take the installation out of; <c>null</c> leaves every registry as it is.
    /// </summary>
    public InstallationRegistry? Registry { get; set; }

    /// <summary>
    /// Gets or sets the NuGet package cache to remove the packages of <c>packages/</c> from, exactly their ids and
    /// versions; <c>null</c> leaves the cache alone. A package left there shadows a rebuilt one of the same version.
    /// </summary>
    public string? PackageCache { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the files the user may have edited (<c>config/</c>, the
    /// isolation files, the engine manifest) go too, edited or not.
    /// </summary>
    public bool IncludeEdited { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the build output (<c>artifacts/</c>) goes too.
    /// </summary>
    public bool IncludeArtifacts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to only tell what would be removed.
    /// </summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// What <see cref="EngineUninstaller.Uninstall"/> did, or would do on a dry run.
/// </summary>
public sealed class UninstallReport
{
    /// <summary>Gets the orchestra versions the installation was registered for.</summary>
    public List<string> Unregistered { get; } = new();

    /// <summary>Gets the files and folders removed.</summary>
    public List<string> Removed { get; } = new();

    /// <summary>Gets what was kept, with the reason.</summary>
    public List<string> Kept { get; } = new();
}

/// <summary>
/// Undoes <see cref="EngineInstaller"/>: the installation goes back to a plain checkout of StellarEngine-Core and
/// the machine forgets it.
/// </summary>
/// <remarks>
/// <para>
/// Only what the installer and the build create is removed. Content of the user is never touched: modules and
/// tools in their folders (repositories of their own), games in <c>projects/</c>. A folder the installer made is
/// removed only once it is empty. A file the installer made from a template is removed only while it is as the
/// template made it, unless <see cref="UninstallOptions.IncludeEdited"/>.
/// </para>
/// <para>
/// Nothing is followed through a link: a junction or a symbolic link is removed as a link, never walked into.
/// </para>
/// </remarks>
public static class EngineUninstaller
{
    private static readonly Regex PackageFileName =
        new(@"^(?<id>.+?)\.(?<version>\d+\.\d+\.\d+(?:[-.][0-9A-Za-z.-]+)?)\.s?nupkg$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Uninstalls in place.
    /// </summary>
    /// <param name="installation">The installation: a checkout of StellarEngine-Core.</param>
    /// <param name="logger">The output.</param>
    /// <param name="options">What else goes.</param>
    /// <returns>What was done.</returns>
    /// <exception cref="ToolException">Thrown if the directory is not a checkout of StellarEngine-Core.</exception>
    public static UninstallReport Uninstall(StellarInstallation installation, IToolLogger logger,
        UninstallOptions options)
    {
        if (!installation.HasCoreSources)
            throw new ToolException(
                $"{installation.Root} is not a checkout of StellarEngine-Core: there is no installation to remove");

        var report = new UninstallReport();
        var verb = options.DryRun ? "Would remove" : "Removed";

        // 1. The machine forgets it
        if (options.Registry is { } registry)
        {
            if (options.DryRun)
                report.Unregistered.AddRange(registry.List()
                    .Where(entry => SamePath(entry.Root, installation.Root))
                    .Select(entry => entry.OrchesterVersion));
            else
                report.Unregistered.AddRange(registry.Unregister(installation));

            foreach (var version in report.Unregistered)
                logger.Info($"{(options.DryRun ? "Would unregister" : "Unregistered")} orchestra {version} from {registry.File}");
        }

        // 2. The packages of this installation in the NuGet cache, before packages/ is gone
        if (options.PackageCache is { } cache && Directory.Exists(installation.Packages))
            foreach (var package in Directory.GetFiles(installation.Packages, "*.nupkg"))
            {
                var match = PackageFileName.Match(Path.GetFileName(package));
                if (!match.Success)
                    continue;

                var cached = Path.Combine(cache, match.Groups["id"].Value.ToLowerInvariant(),
                    match.Groups["version"].Value.ToLowerInvariant());
                Remove(cached, options, report, logger, verb);
            }

        // 3. Generated: always
        Remove(installation.Packages, options, report, logger, verb);
        Remove(Path.GetDirectoryName(installation.ModuleCatalog)!, options, report, logger, verb);
        Remove(installation.ModuleDocs, options, report, logger, verb);

        // 4. Made from the template: while unchanged
        var template = Path.Combine(installation.Templates, EngineInstaller.TemplateName);
        if (Directory.Exists(template))
            foreach (var pair in Templates.TemplateRenderer.Preview(template, installation.Root,
                         new Dictionary<string, string> { ["OrchesterVersion"] = StellarVersions.Orchester }))
                RemoveIfUnchanged(pair.Key, pair.Value, options, report, logger, verb);

        // 5. The manifest: while it is the one the installer created
        if (File.Exists(installation.EngineManifest))
        {
            var created = StellarJson.Serialize(new EngineManifest
                { OrchesterVersion = StellarVersions.Orchester });
            RemoveIfUnchanged(installation.EngineManifest, created, options, report, logger, verb);
        }

        // 6. The build output, on request. Not from under a running tool
        if (options.IncludeArtifacts)
        {
            if (IsInside(AppContext.BaseDirectory, installation.Artifacts))
                Keep(installation.Artifacts, "this tool runs from it: delete it after the tool exits", report, logger);
            else
                Remove(installation.Artifacts, options, report, logger, verb);
        }

        // 7. The folders of the installer, once empty
        foreach (var directory in new[]
                 {
                     installation.ResourceCompilers, installation.Tools, installation.Config, installation.Manifests,
                     installation.Projects, installation.Modules
                 })
            RemoveIfEmpty(directory, options, report, logger, verb);

        logger.Info(options.DryRun
            ? $"Dry run: {report.Removed.Count} item(s) would be removed from {installation.Root}"
            : $"StellarEngine uninstalled from {installation.Root}: the checkout of the core stays");
        return report;
    }

    private static void RemoveIfUnchanged(string file, string created, UninstallOptions options,
        UninstallReport report, IToolLogger logger, string verb)
    {
        if (!File.Exists(file))
            return;

        if (!options.IncludeEdited && !string.Equals(Normalize(File.ReadAllText(file)), Normalize(created),
                StringComparison.Ordinal))
        {
            Keep(file, "edited since the install (pass --all to remove it)", report, logger);
            return;
        }

        Remove(file, options, report, logger, verb);
    }

    private static void RemoveIfEmpty(string directory, UninstallOptions options, UninstallReport report,
        IToolLogger logger, string verb)
    {
        if (!Directory.Exists(directory))
            return;

        // On a dry run nothing was removed yet: a folder counts as empty if all it holds would go
        var remaining = Directory.EnumerateFileSystemEntries(directory)
            .Where(entry => !options.DryRun || !report.Removed.Contains(entry, PathComparer))
            .ToList();
        if (remaining.Count != 0)
        {
            Keep(directory, $"holds {remaining.Count} item(s) that are not the installer's", report, logger);
            return;
        }

        Remove(directory, options, report, logger, verb);
    }

    private static void Remove(string path, UninstallOptions options, UninstallReport report, IToolLogger logger,
        string verb)
    {
        var isFile = File.Exists(path);
        if (!isFile && !Directory.Exists(path))
            return;

        if (!options.DryRun)
        {
            if (isFile)
                File.Delete(path);
            else
                DeleteDirectory(new DirectoryInfo(path));
        }

        report.Removed.Add(path);
        logger.Info($"{verb} {path}");
    }

    private static void Keep(string path, string reason, UninstallReport report, IToolLogger logger)
    {
        report.Kept.Add($"{path}: {reason}");
        logger.Warning($"Kept {path}: {reason}");
    }

    /// <summary>
    /// Deletes a folder without following links: a link inside, or the folder itself if it is one, is removed as a
    /// link and what it points at stays.
    /// </summary>
    private static void DeleteDirectory(DirectoryInfo directory)
    {
        if (!directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
            foreach (var entry in directory.EnumerateFileSystemInfos())
            {
                if (entry is DirectoryInfo child)
                {
                    DeleteDirectory(child);
                    continue;
                }

                entry.Attributes &= ~FileAttributes.ReadOnly;
                entry.Delete();
            }

        directory.Attributes &= ~FileAttributes.ReadOnly;
        directory.Delete(false);
    }

    private static StringComparer PathComparer =>
        Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private static bool SamePath(string left, string right)
    {
        return PathComparer.Equals(Path.GetFullPath(left).TrimEnd('/', '\\'),
            Path.GetFullPath(right).TrimEnd('/', '\\'));
    }

    private static bool IsInside(string path, string directory)
    {
        var prefix = Path.GetFullPath(directory).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(prefix,
            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").Trim();
    }
}
