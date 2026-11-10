using System;
using System.IO;
using System.Security;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Modules;

/// <summary>
/// Makes the local feed reachable from a module folder. Stellar.Sdk itself comes from that feed, before any
/// property of the SDK exists, so NuGet must know it from its own configuration.
/// </summary>
/// <remarks>
/// A module under the root of the installation needs nothing: NuGet finds the NuGet.config of the root by walking
/// up. A module elsewhere gets a NuGet.config of its own, unless it has one.
/// </remarks>
public static class ModuleFeed
{
    /// <summary>
    /// Writes a NuGet.config pointing at the local feed into a module folder outside the installation.
    /// </summary>
    /// <param name="installation">The installation.</param>
    /// <param name="directory">The module folder.</param>
    /// <param name="logger">The output.</param>
    /// <returns><c>true</c> if a file was written.</returns>
    public static bool EnsureReachable(StellarInstallation installation, string directory, IToolLogger logger)
    {
        var root = installation.Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (Path.GetFullPath(directory).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return false;

        var path = Path.Combine(directory, "NuGet.config");
        if (File.Exists(path))
            return false;

        FileWriter.WriteAllText(path,
            $"""
             <?xml version="1.0" encoding="utf-8"?>
             <!-- Written by 'stellar modules': the local feed of the installation at {SecurityElement.Escape(installation.Root)} -->
             <configuration>
                 <packageSources>
                     <add key="StellarLocal" value="{SecurityElement.Escape(installation.Packages)}"/>
                 </packageSources>
                 <packageSourceMapping>
                     <packageSource key="StellarLocal">
                         <package pattern="Stellar.*"/>
                     </packageSource>
                     <packageSource key="nuget.org">
                         <package pattern="*"/>
                     </packageSource>
                 </packageSourceMapping>
             </configuration>

             """);

        logger.Info($"Created {path}: the module lives outside the installation and needs its local feed");
        return true;
    }
}
