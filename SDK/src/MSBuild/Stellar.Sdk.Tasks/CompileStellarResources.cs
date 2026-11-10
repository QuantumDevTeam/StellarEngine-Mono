// ReSharper disable UnusedType.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Stellar.Tools.Environment;
using Stellar.Tools.Projects;
using Stellar.Tools.Resources;

namespace Stellar.Sdk.Tasks;

/// <summary>
/// Compiles the resources of a project into the engine format.
/// </summary>
public sealed class CompileStellarResources
    : StellarTask
{
    /// <summary>Gets or sets the project configuration file.</summary>
    [Required]
    public string ConfigurationFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the directory for compiled resources.</summary>
    [Required]
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets the installation, for its shared resource compilers.</summary>
    public string StellarRoot { get; set; } = string.Empty;

    /// <summary>Gets the compiled resources, to be copied to the output.</summary>
    [Output]
    public ITaskItem[] CompiledResources { get; private set; } = [];

    /// <inheritdoc/>
    protected override void Run()
    {
        var project = StellarProject.Load(ConfigurationFile);
        var sources = ProjectFileResolver.Resolve(project).Resources;

        var compilers = ResourceCompilerRegistry.CreateDefault();

        if (!string.IsNullOrEmpty(StellarRoot))
            compilers.AddFromDirectory(new StellarInstallation(StellarRoot).ResourceCompilers, this);

        foreach (var assembly in project.Configuration.Resources?.Compilers ?? [])
            compilers.AddFrom(Path.Combine(project.Directory, assembly), this);

        var outputRoot = Path.GetFullPath(Path.Combine(project.Directory, OutputDirectory))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var result = new ResourcePipeline(compilers, this).Compile(sources, outputRoot);

        Log.LogMessage(MessageImportance.High, $"Resource compilation result: " +
                                               $"UpToDate - {result.UpToDate}, " +
                                               $"Removed -  {result.Removed}, " +
                                               $"Compiled - {result.Compiled}");

        CompiledResources = result.Outputs
            .Select(output =>
            {
                var item = new TaskItem(output);
                item.SetMetadata("Link", Path.Combine("resources", output.Substring(outputRoot.Length + 1)));
                item.SetMetadata("CopyToOutputDirectory", "PreserveNewest");
                return (ITaskItem)item;
            })
            .ToArray();
    }
}
