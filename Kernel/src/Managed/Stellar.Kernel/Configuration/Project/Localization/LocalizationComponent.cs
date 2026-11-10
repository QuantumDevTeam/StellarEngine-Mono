using System;

namespace Stellar.Kernel.Configuration.Project.Localization;

/// <summary>
/// Localization runtime data.
/// </summary>
public class LocalizationComponent
    : ConfigurationComponent
{
    /// <summary>
    /// The value of <see cref="Kind"/> for this component.
    /// </summary>
    public const string ComponentKind = "Localization";

    /// <inheritdoc/>
    public override string Kind => ComponentKind;

    /// <summary>
    /// Gets the build type of this component. Always <see cref="ConfigurationComponentBuildType.EngineComponent"/>.
    /// </summary>
    public override ConfigurationComponentBuildType ComponentBuildType =>
        ConfigurationComponentBuildType.EngineComponent;

    /// <summary>
    /// Gets or sets the default culture.
    /// </summary>
    public string DefaultCulture { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the array of supported cultures.
    /// </summary>
    public string[] SupportedCultures { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the array of localization index files.
    /// </summary>
    public LocalizationIndexData[] LocalizationIndexFiles { get; set; } = Array.Empty<LocalizationIndexData>();
}
