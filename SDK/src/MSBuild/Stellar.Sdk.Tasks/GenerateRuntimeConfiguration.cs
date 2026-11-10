// ReSharper disable UnusedType.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Stellar.Kernel.Modules;
using Stellar.Tools.Build;
using Stellar.Tools.Environment;
using Stellar.Tools.Projects;

namespace Stellar.Sdk.Tasks;

/// <summary>
/// Generates the runtime configuration file of a project.
/// </summary>
public sealed class GenerateRuntimeConfiguration
    : StellarTask
{
    /// <summary>Gets or sets the project configuration file.</summary>
    [Required]
    public string ConfigurationFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the file to write.</summary>
    [Required]
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the project assembly.</summary>
    [Required]
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the project version.</summary>
    [Required]
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets the Stellar Orchester version.</summary>
    [Required]
    public string OrchesterVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the engine version.</summary>
    [Required]
    public string EngineVersion { get; set; } = string.Empty;

    /// <summary>Gets or sets the company name.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the modules of the installation: <c>@(StellarModuleInfo)</c>, with the metadata <c>Level</c>,
    /// <c>Version</c> and <c>Dependencies</c>.
    /// </summary>
    public ITaskItem[] Modules { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>Gets or sets the modules the project depends on directly: <c>@(StellarDependency)</c>.</summary>
    public ITaskItem[] Dependencies { get; set; } = Array.Empty<ITaskItem>();

    private System.Collections.Generic.IReadOnlyList<Kernel.Configuration.Modules.RuntimeModuleData> PlanModules()
    {
        var catalog = Modules.Select(item => new CatalogModule(
            item.ItemSpec,
            Enum.TryParse<ModuleLevel>(item.GetMetadata("Level"), out var level) ? level : ModuleLevel.HighLevel,
            item.GetMetadata("Version").Trim('[', ']'),
            item.GetMetadata("Dependencies")
                .Split([';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(dependency => dependency.Trim())
                .ToArray()));

        var plan = RuntimeModulePlanner.Plan(catalog, Dependencies.Select(item => item.ItemSpec), out var unknown);

        foreach (var module in unknown)
            Log.LogWarning($"The module {module} is not in the catalog: the runtime will not load it");

        return plan;
    }

    /// <inheritdoc/>
    protected override void Run()
    {
        var project = StellarProject.Load(ConfigurationFile);

        var configuration = RuntimeConfigurationBuilder.Build(project, ProjectFileResolver.Resolve(project),
            new RuntimeConfigurationInput
            {
                AssemblyName = AssemblyName,
                CompanyName = CompanyName,
                Version = Version,
                OrchesterVersion = OrchesterVersion,
                EngineVersion = EngineVersion,
                Modules = PlanModules()
            });

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(OutputFile))!);
        StellarJson.Write(OutputFile, configuration);
    }
}
