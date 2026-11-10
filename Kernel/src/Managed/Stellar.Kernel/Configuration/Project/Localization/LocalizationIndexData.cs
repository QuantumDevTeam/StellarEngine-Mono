using Stellar.Kernel.Configuration.Project.Assets;

namespace Stellar.Kernel.Configuration.Project.Localization;

/// <summary>
/// Information about a localization index file.
/// </summary>
/// <remarks>Inherits <see cref="AssetData"/> and has its path parameters.</remarks>
public class LocalizationIndexData
    : AssetData
{
    /// <summary>
    /// Gets or sets the name of the culture.
    /// </summary>
    public string Culture { get; set; } = string.Empty;
}
