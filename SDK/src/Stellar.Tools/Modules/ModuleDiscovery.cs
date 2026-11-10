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

        var found = new List<string>();

        foreach (var directory in settings.Directories.Select(installation.Resolve).Where(Directory.Exists))
            Search(directory, settings.Depth, found);

        return found.Distinct().OrderBy(path => path, System.StringComparer.Ordinal).ToList();
    }

    private static void Search(string directory, int depth, List<string> found)
    {
        if (depth <= 0)
            return;

        foreach (var child in Directory.GetDirectories(directory).OrderBy(path => path, System.StringComparer.Ordinal))
        {
            if (Path.GetFileName(child).StartsWith("."))
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
