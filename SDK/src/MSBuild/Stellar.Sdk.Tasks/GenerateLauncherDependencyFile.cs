// ReSharper disable UnusedType.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Stellar.Tools.Build;
using Stellar.Tools.Environment;

namespace Stellar.Sdk.Tasks;

/// <summary>
/// Generates the <c>.deps.json</c> of the launcher of an executable module: the launcher, the runtime, the Kernel.
/// </summary>
public sealed class GenerateLauncherDependencyFile
    : StellarTask
{
    /// <summary>Gets or sets the framework: <c>$(TargetFrameworkMoniker)</c>.</summary>
    [Required]
    public string TargetFrameworkMoniker { get; set; } = string.Empty;

    /// <summary>Gets or sets the simple name of the launcher assembly.</summary>
    [Required]
    public string Launcher { get; set; } = string.Empty;

    /// <summary>Gets or sets the version of the launcher.</summary>
    public string LauncherVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the assemblies the launcher starts with: the files, with an optional <c>Version</c> metadata.
    /// </summary>
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>Gets or sets the file to write.</summary>
    [Required]
    public string OutputFile { get; set; } = string.Empty;

    /// <inheritdoc/>
    protected override void Run()
    {
        var assemblies = Assemblies
            .Select(item => new LauncherAssembly(
                item.GetMetadata("Filename"),
                string.IsNullOrEmpty(item.GetMetadata("Version")) ? "1.0.0" : item.GetMetadata("Version")))
            .GroupBy(assembly => assembly.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        FileWriter.WriteAllText(OutputFile, LauncherDependencyFile.Render(TargetFrameworkMoniker,
            new LauncherAssembly(Launcher, string.IsNullOrEmpty(LauncherVersion) ? "1.0.0" : LauncherVersion),
            assemblies));
    }
}
