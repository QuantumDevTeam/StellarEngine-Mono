using System;

namespace Stellar.Kernel.Configuration.Project.Assets;

/// <summary>
/// A configuration component that describes asset bundles used by the engine.
/// </summary>
/// <remarks>
/// Inherits from <see cref="ConfigurationComponent"/> and specifies external and embedded assets.
/// This component is built as an <see cref="ConfigurationComponentBuildType.EngineComponent"/>.
/// </remarks>
public class AssetsComponent
    : ConfigurationComponent
{
    /// <summary>
    /// The value of <see cref="Kind"/> for this component.
    /// </summary>
    public const string ComponentKind = "Assets";

    /// <inheritdoc/>
    public override string Kind => ComponentKind;

    /// <summary>
    /// Gets the build type of this component. Always <see cref="ConfigurationComponentBuildType.EngineComponent"/>.
    /// </summary>
    public override ConfigurationComponentBuildType ComponentBuildType =>
        ConfigurationComponentBuildType.EngineComponent;

    /// <summary>
    /// Gets or sets an array of assets that are loaded from external files (not embedded).
    /// </summary>
    public AssetData[] ExternalAssets { get; set; } = Array.Empty<AssetData>();

    /// <summary>
    /// Gets or sets an array of assets that are embedded in the compiled artifact (e.g., resources).
    /// </summary>
    public AssetData[] EmbeddedAssets { get; set; } = Array.Empty<AssetData>();
}
