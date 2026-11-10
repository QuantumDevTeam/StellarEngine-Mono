namespace Stellar.Tools.Configuration.Project.Localizations;

/// <summary>
/// Represents the .stellar.project[Project][Localizations] field.
/// </summary>
public class LocalizationsConfigurationObject
{
    /// <summary>
    /// Default culture to use.
    /// </summary>
    public string? DefaultCulture { get; set; }

    /// <summary>
    /// Supported cultures.
    /// </summary>
    public string[]? Cultures { get; set; }

    /// <summary>
    /// Glob patterns of the index files for localizations.
    /// </summary>
    public string[]? IndexFiles { get; set; }
}
