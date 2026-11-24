using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stellar.Kernel.Configuration.Manifest;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Modules;

/// <summary>
/// Finds module folders: a folder that holds a <c>&lt;Id&gt;.stellar.module</c> file is a module, and its
/// subfolders are its own business.
/// </summary>
public static class ModuleDiscovery
{
    /// <summary>
    /// Finds the descriptor files below the searched folders of an installation.
    /// </summary>
    /// <param name="installation">The installation.</param>
    /// <param name="settings">What to search.</param>
    /// <returns>The descriptor files, sorted.</returns>
    public static IReadOnlyList<string> Discover(StellarInstallation installation, ModuleDiscoverySettings settings)
    {
        if (!settings.Enabled)
            return new string[0];

        return Discover(settings.Directories.Select(installation.Resolve), settings.Depth);
    }

    /// <summary>
    /// Finds the descriptor files below some folders: the search of an installation, and of a solution.
    /// </summary>
    /// <param name="directories">The folders to search; those that do not exist are skipped.</param>
    /// <param name="depth">How many levels below each folder a module may be.</param>
    /// <returns>The descriptor files, sorted.</returns>
    public static IReadOnlyList<string> Discover(IEnumerable<string> directories, int depth)
    {
        var found = new List<string>();

        foreach (var directory in directories.Where(Directory.Exists))
            Search(directory, depth, found);

        return found.Distinct().OrderBy(path => path, System.StringComparer.Ordinal).ToList();
    }

    private static void Search(string directory, int depth, List<string> found)
    {
        if (depth <= 0)
            return;

        foreach (var child in Directory.GetDirectories(directory).OrderBy(path => path, System.StringComparer.Ordinal))
        {
            var name = Path.GetFileName(child);

            // Hidden folders and build output hold no modules of theirs
            if (name.StartsWith(".") || name.Equals("bin", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("obj", System.StringComparison.OrdinalIgnoreCase))
                continue;

            var descriptor = ModuleDescriptorFile.Find(child);

            // A module ends the search: nothing inside it is another module of the installation
            if (descriptor is not null)
                found.Add(descriptor);
            else
                Search(child, depth - 1, found);
        }
    }
}
