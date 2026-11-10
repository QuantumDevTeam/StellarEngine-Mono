namespace Stellar.Tools.Configuration.Project.Resources;

/// <summary>
/// Represents the .stellar.project[Project][Resources] field.
/// </summary>
public class ResourcesConfigurationObject
{
    /// <summary>
    /// Glob patterns of the source files to compile.
    /// </summary>
    public string[]? Include { get; set; }

    /// <summary>
    /// Glob patterns of the source files to skip.
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <summary>
    /// Additional assemblies with resource compilers, relative to the project.
    /// </summary>
    public string[]? Compilers { get; set; }
}
