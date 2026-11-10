using System.IO;
using System.Linq;
using Stellar.Tools.Configuration;
using Stellar.Tools.Configuration.Project;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Projects;

/// <summary>
/// A Stellar project on disk: its directory and its parsed <c>.stellar.project</c> file.
/// </summary>
public sealed class StellarProject
{
    /// <summary>
    /// The extension of the project configuration file.
    /// </summary>
    public const string ConfigurationExtension = ".stellar.project";

    /// <summary>
    /// The keyword for the conventional entry point.
    /// </summary>
    public const string DefaultEntryPoint = "default";

    private StellarProject(string name, string directory, string configurationPath, StellarConfigurationFile file)
    {
        Name = name;
        Directory = directory;
        ConfigurationPath = configurationPath;
        File = file;
    }

    /// <summary>Gets the name of the project.</summary>
    public string Name { get; }

    /// <summary>Gets the project directory.</summary>
    public string Directory { get; }

    /// <summary>Gets the path of the configuration file.</summary>
    public string ConfigurationPath { get; }

    /// <summary>Gets the parsed configuration file.</summary>
    public StellarConfigurationFile File { get; }

    /// <summary>Gets the Project section. It is never <c>null</c>, a missing section means defaults.</summary>
    public ProjectConfigurationObject Configuration => File.Project ?? new ProjectConfigurationObject();

    /// <summary>
    /// Resolves the full type name of the entry point.
    /// </summary>
    /// <param name="assemblyName">The name of the project assembly.</param>
    /// <returns>The type name.</returns>
    public string ResolveEntryPoint(string assemblyName)
    {
        var value = Configuration.StellarEntryPoint?.Trim();

        return string.IsNullOrEmpty(value) || value == DefaultEntryPoint
            ? $"{assemblyName}.EntryPoint"
            : value!;
    }

    /// <summary>
    /// Loads a project by its configuration file.
    /// </summary>
    /// <param name="configurationPath">The <c>.stellar.project</c> file.</param>
    /// <returns>The project.</returns>
    /// <exception cref="ToolException">Thrown if the file is missing or malformed.</exception>
    public static StellarProject Load(string configurationPath)
    {
        var fullPath = Path.GetFullPath(configurationPath);
        var fileName = Path.GetFileName(fullPath);
        var name = fileName.EndsWith(ConfigurationExtension, System.StringComparison.OrdinalIgnoreCase)
            ? fileName.Substring(0, fileName.Length - ConfigurationExtension.Length)
            : Path.GetFileNameWithoutExtension(fileName);

        return new StellarProject(name, Path.GetDirectoryName(fullPath)!, fullPath,
            StellarJson.Read<StellarConfigurationFile>(fullPath));
    }

    /// <summary>
    /// Finds the single project of a directory.
    /// </summary>
    /// <param name="directory">The directory to search.</param>
    /// <returns>The project.</returns>
    /// <exception cref="ToolException">Thrown if there is no project or more than one.</exception>
    public static StellarProject Find(string directory)
    {
        var candidates = System.IO.Directory.GetFiles(directory, "*" + ConfigurationExtension);

        return candidates.Length switch
        {
            0 => throw new ToolException($"No {ConfigurationExtension} file in {directory}"),
            1 => Load(candidates[0]),
            _ => throw new ToolException(
                $"Several projects in {directory}: {string.Join(", ", candidates.Select(Path.GetFileName))}")
        };
    }
}
