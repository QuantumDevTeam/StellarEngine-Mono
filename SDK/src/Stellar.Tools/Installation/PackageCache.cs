using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Installation;

/// <summary>
/// The NuGet package cache of the user, and the packages of an installation in it.
/// </summary>
/// <remarks>
/// NuGet unpacks a package version once and never looks at the feed for it again. The engine packs its packages
/// again under the same version while it is developed, so a cached copy would hide the fresh one from every game:
/// <see cref="RefreshStale"/> takes such copies out after a pack, and the next restore unpacks the new ones.
/// </remarks>
public static class PackageCache
{
    private static readonly Regex PackageFileName =
        new(@"^(?<id>.+?)\.(?<version>\d+\.\d+\.\d+(?:[-.][0-9A-Za-z.-]+)?)\.nupkg$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Gets the cache of the user: $NUGET_PACKAGES, or ~/.nuget/packages.
    /// </summary>
    public static string Location()
    {
        var fromVariable = System.Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        return !string.IsNullOrWhiteSpace(fromVariable)
            ? Path.GetFullPath(fromVariable)
            : Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nuget", "packages");
    }

    /// <summary>
    /// Takes out of the cache every package of a feed that was packed after it was unpacked there.
    /// </summary>
    /// <param name="feed">The folder of the packages: <c>packages/</c> of the installation.</param>
    /// <param name="cache">The cache; <see cref="Location"/> if <c>null</c>.</param>
    /// <param name="logger">Tells what was taken out, and what could not be (a running IDE may hold a file).</param>
    /// <returns>The folders taken out of the cache.</returns>
    public static IReadOnlyList<string> RefreshStale(string feed, string? cache, IToolLogger logger)
    {
        cache ??= Location();
        var removed = new List<string>();
        if (!Directory.Exists(feed) || !Directory.Exists(cache)) return removed;

        foreach (var package in Directory.GetFiles(feed, "*.nupkg"))
        {
            var match = PackageFileName.Match(Path.GetFileName(package));
            if (!match.Success) continue;

            var cached = Path.Combine(cache, match.Groups["id"].Value.ToLowerInvariant(), match.Groups["version"].Value.ToLowerInvariant());
            var marker = Path.Combine(cached, ".nupkg.metadata");
            if (!Directory.Exists(cached)) continue;
            if (File.Exists(marker) && File.GetLastWriteTimeUtc(marker) >= File.GetLastWriteTimeUtc(package)) continue;

            try
            {
                // Moved away first: a folder with a held file does not move, so it is left whole, never half deleted
                var away = cached + ".stale-" + Guid.NewGuid().ToString("N");
                Directory.Move(cached, away);
                removed.Add(cached);
                try
                {
                    Directory.Delete(away, true);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    logger.Verbose($"Left {away} to delete later: {exception.Message}");
                }

                logger.Verbose($"Took the old {Path.GetFileNameWithoutExtension(package)} out of the NuGet cache");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.Warning($"The NuGet cache still has the old {Path.GetFileNameWithoutExtension(package)}, held by a running process (an IDE?): close it and pack again, or games keep the old one. {exception.Message}");
            }
        }

        return removed;
    }
}
