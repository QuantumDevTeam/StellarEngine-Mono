// ReSharper disable UnusedType.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Stellar.Tools.Build;
using Stellar.Tools.Projects;

namespace Stellar.Sdk.Tasks;

/// <summary>
/// Reads the <c>.stellar.project</c> file and exposes what it selects as MSBuild items.
/// </summary>
public sealed class ResolveStellarProject
    : StellarTask
{
    /// <summary>Gets or sets the project configuration file.</summary>
    [Required]
    public string ConfigurationFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the project assembly.</summary>
    [Required]
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>Gets the full name of the entry point class.</summary>
    [Output]
    public string EntryPoint { get; private set; } = string.Empty;

    /// <summary>Gets the assets copied to the output.</summary>
    [Output]
    public ITaskItem[] ExternalAssets { get; private set; } = [];

    /// <summary>Gets the assets embedded into the assembly.</summary>
    [Output]
    public ITaskItem[] EmbeddedAssets { get; private set; } = [];

    /// <summary>Gets the localization index files.</summary>
    [Output]
    public ITaskItem[] LocalizationFiles { get; private set; } = [];

    private static ITaskItem ToExternalItem(ProjectFile file)
    {
        var item = new TaskItem(file.FullPath);
        item.SetMetadata("Link", file.RelativePath);
        item.SetMetadata("CopyToOutputDirectory", "PreserveNewest");
        return item;
    }

    private static ITaskItem ToEmbeddedItem(StellarProject project, ProjectFile file)
    {
        var item = new TaskItem(file.FullPath);
        item.SetMetadata("LogicalName", RuntimeConfigurationBuilder.EmbeddedNameFor(project.Name, file.RelativePath));
        return item;
    }

    /// <inheritdoc/>
    protected override void Run()
    {
        var project = StellarProject.Load(ConfigurationFile);
        var files = ProjectFileResolver.Resolve(project);

        EntryPoint = project.ResolveEntryPoint(AssemblyName);

        ExternalAssets = files.ExternalAssets
            .Select(ToExternalItem)
            .ToArray();

        EmbeddedAssets = files.EmbeddedAssets
            .Select(file => ToEmbeddedItem(project, file))
            .ToArray();

        LocalizationFiles = files.LocalizationFiles
            .Select(localization => ToExternalItem(localization.File))
            .ToArray();
    }
}
