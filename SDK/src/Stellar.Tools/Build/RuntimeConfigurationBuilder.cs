using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Configuration.Project.Assets;
using Stellar.Kernel.Configuration.Project.Localization;
using Stellar.Tools.Projects;

namespace Stellar.Tools.Build;

/// <summary>
/// The build-time facts that are not part of the <c>.stellar.project</c> file.
/// </summary>
public sealed class RuntimeConfigurationInput
{
    /// <summary>Gets the name of the project assembly.</summary>
    public string AssemblyName { get; init; } = string.Empty;

    /// <summary>Gets the company name.</summary>
    public string CompanyName { get; init; } = string.Empty;

    /// <summary>Gets the project version.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>Gets the Stellar Orchester version.</summary>
    public string OrchesterVersion { get; init; } = string.Empty;

    /// <summary>Gets the engine version.</summary>
    public string EngineVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets the logical name embedded assets get: a function of their relative path.
    /// </summary>
    public Func<string, string>? EmbeddedName { get; init; }

    /// <summary>
    /// Gets the modules the application loads, in load order (<see cref="RuntimeModulePlanner"/>).
    /// </summary>
    public IReadOnlyList<RuntimeModuleData> Modules { get; init; } = Array.Empty<RuntimeModuleData>();
}

/// <summary>
/// Builds the runtime configuration of a project.
/// </summary>
public static class RuntimeConfigurationBuilder
{
    /// <summary>
    /// The suffix of the generated file: <c>{Project}.stellar.runtime.config.json</c>.
    /// </summary>
    public const string FileSuffix = ".stellar.runtime.config.json";

    /// <summary>
    /// Gets the name of the runtime configuration file of a project.
    /// </summary>
    public static string FileNameFor(string projectName)
    {
        return projectName + FileSuffix;
    }

    /// <summary>
    /// Gets the logical name of an embedded file: <c>{Project}:{relative path}</c>.
    /// </summary>
    public static string EmbeddedNameFor(string projectName, string relativePath)
    {
        return $"{projectName}:{relativePath}";
    }

    private static Dictionary<string, string> BuildSettings(StellarProject project)
    {
        var settings = new Dictionary<string, string>();

        foreach (var pair in project.File.Runtime ?? new Dictionary<string, JToken>())
            settings[pair.Key] = pair.Value.Type == JTokenType.String
                ? pair.Value.Value<string>()!
                : pair.Value.ToString(Newtonsoft.Json.Formatting.None);

        return settings;
    }

    /// <summary>
    /// Builds the configuration.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="files">The files selected by the project configuration.</param>
    /// <param name="input">The build-time facts.</param>
    /// <returns>The configuration, ready to be serialized.</returns>
    public static RuntimeConfiguration Build(
        StellarProject project, ResolvedProjectFiles files, RuntimeConfigurationInput input)
    {
        var embeddedName = input.EmbeddedName ?? (path => EmbeddedNameFor(project.Name, path));

        return new RuntimeConfiguration
        {
            ProjectName = project.Name,
            CompanyName = input.CompanyName,
            Version = input.Version,
            StellarOrchesterVersion = input.OrchesterVersion,
            StellarEngineVersion = input.EngineVersion,
            EntryPoint = project.ResolveEntryPoint(input.AssemblyName),
            BuildDate = DateTime.UtcNow.ToString("O"),
            Settings = BuildSettings(project),
            Components =
            [
                new AssetsComponent
                {
                    ExternalAssets = files.ExternalAssets
                        .Select(file => new AssetData { Path = file.RelativePath, OriginalPath = file.RelativePath })
                        .ToArray(),
                    EmbeddedAssets = files.EmbeddedAssets
                        .Select(file => new AssetData
                        {
                            Path = embeddedName(file.RelativePath),
                            OriginalPath = file.RelativePath
                        })
                        .ToArray()
                },
                new LocalizationComponent
                {
                    DefaultCulture = files.DefaultCulture ?? "en",
                    SupportedCultures = files.SupportedCultures.ToArray(),
                    LocalizationIndexFiles = files.LocalizationFiles
                        .Select(localization => new LocalizationIndexData
                        {
                            Path = localization.File.RelativePath,
                            OriginalPath = localization.File.RelativePath,
                            Culture = localization.Culture
                        })
                        .ToArray()
                },
                new ModulesComponent { Modules = input.Modules.ToArray() }
            ]
        };
    }
}
