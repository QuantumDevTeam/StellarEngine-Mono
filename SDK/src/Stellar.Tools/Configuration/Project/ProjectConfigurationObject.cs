using Stellar.Tools.Configuration.Project.Assets;
using Stellar.Tools.Configuration.Project.Localizations;
using Stellar.Tools.Configuration.Project.Resources;

namespace Stellar.Tools.Configuration.Project;

/// <summary>
/// Represents the .stellar.project[Project] field.
/// </summary>
public class ProjectConfigurationObject
{
    /// <summary>
    /// The entry point of the module: a full type name, or "default" for {AssemblyName}.EntryPoint.
    /// </summary>
    public string? StellarEntryPoint { get; set; }

    /// <summary>
    /// Assets of the module: files copied or embedded as they are.
    /// </summary>
    public AssetsConfigurationObject? Assets { get; set; }

    /// <summary>
    /// Resources of the module: files compiled into the engine format.
    /// </summary>
    public ResourcesConfigurationObject? Resources { get; set; }

    /// <summary>
    /// Localizations of the module.
    /// </summary>
    public LocalizationsConfigurationObject? Localizations { get; set; }
}
