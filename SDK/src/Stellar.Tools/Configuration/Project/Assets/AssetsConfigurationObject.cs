namespace Stellar.Tools.Configuration.Project.Assets;

/// <summary>
/// Represents the .stellar.project[Project][Assets] field. Every list is an optional set of glob patterns.
/// </summary>
public class AssetsConfigurationObject
{
    /// <summary>
    /// Included assets.
    /// </summary>
    public string[]? Include { get; set; }

    /// <summary>
    /// Excluded assets.
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <summary>
    /// Assets embedded into the DLL file.
    /// </summary>
    public string[]? Embedded { get; set; }
}
