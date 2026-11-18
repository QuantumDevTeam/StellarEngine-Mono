using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Solutions;
using Stellar.Tools.Templates;

namespace Stellar.Tools.Projects;

/// <summary>
/// Creates projects (games, libraries) from <c>Shared/Templates/Projects</c>.
/// </summary>
public sealed class ProjectScaffolder
{
    private static readonly Regex ProjectNamePattern = new("^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.Compiled);

    private readonly string _templatesDirectory;
    private readonly TemplateRenderer _renderer;
    private readonly IToolLogger _logger;

    /// <summary>
    /// Creates a scaffolder.
    /// </summary>
    /// <param name="templatesDirectory">The directory with one subdirectory per template.</param>
    /// <param name="logger">The output.</param>
    public ProjectScaffolder(string templatesDirectory, IToolLogger logger)
    {
        _templatesDirectory = templatesDirectory;
        _renderer = new TemplateRenderer(logger);
        _logger = logger;
    }

    /// <summary>
    /// Lists the available templates.
    /// </summary>
    public IReadOnlyList<string> ListTemplates()
    {
        return TemplateRenderer.List(_templatesDirectory);
    }

    /// <summary>
    /// Creates a project.
    /// </summary>
    /// <param name="template">The name of the template.</param>
    /// <param name="projectName">The name of the new project.</param>
    /// <param name="outputDirectory">The directory to create.</param>
    /// <param name="installation">The installation the project is bound to.</param>
    /// <exception cref="ToolException">Thrown if the name is invalid, the template is unknown or the target exists.</exception>
    /// <param name="flat">Puts the project in the output directory itself, without a solution, as before 0.9.</param>
    /// <param name="references">Reads the references of the project for its solution; MSBuild if <c>null</c>.</param>
    public void Create(string template, string projectName, string outputDirectory, StellarInstallation installation,
        bool flat = false, IProjectReferenceReader? references = null)
    {
        if (!ProjectNamePattern.IsMatch(projectName))
            throw new ToolException($"'{projectName}' is not a valid project name");

        var templateDirectory = Path.Combine(_templatesDirectory, template);
        if (!Directory.Exists(templateDirectory))
            throw new ToolException(
                $"Unknown template '{template}'. Available: {string.Join(", ", ListTemplates())}");

        _renderer.Render(templateDirectory, outputDirectory, new Dictionary<string, string>
        {
            ["ProjectName"] = projectName,
            ["SdkVersion"] = StellarVersions.Sdk,
            ["PackagesDirectory"] = installation.Packages,
            ["StellarRoot"] = installation.Root,
            ["StellarSchemasUri"] = StellarVersions.SchemasUri
        });

        // A template keeps its project in a folder named after it, and its solution files around that folder
        var projectDirectory = Path.Combine(outputDirectory, projectName);
        if (!Directory.Exists(projectDirectory))
        {
            _logger.Info($"Project {projectName} created in {outputDirectory}");
            return;
        }

        if (flat)
        {
            Flatten(outputDirectory, projectDirectory, projectName);
            _logger.Info($"Project {projectName} created in {outputDirectory}");
            return;
        }

        var projects = Directory.GetFiles(projectDirectory, "*.csproj").Select(path => new SolutionProject(path)).ToList();
        new SolutionGenerator(installation.Root, references ?? new MSBuildProjectReferenceReader(), _logger)
            .Generate(Path.Combine(outputDirectory, projectName + SolutionFile.Extension), projects);

        _logger.Info($"Solution {projectName} created in {outputDirectory}, its project in {projectDirectory}");
    }

    // The files of the project go up into the output directory; the files of a solution go away
    private static void Flatten(string outputDirectory, string projectDirectory, string projectName)
    {
        File.Delete(Path.Combine(outputDirectory, projectName + StellarSolution.Extension));
        File.Delete(Path.Combine(outputDirectory, "Directory.Build.props"));

        foreach (var entry in Directory.GetFileSystemEntries(projectDirectory))
        {
            var target = Path.Combine(outputDirectory, Path.GetFileName(entry));
            if (File.Exists(target) || Directory.Exists(target))
                throw new ToolException($"The template has {Path.GetFileName(entry)} twice: it cannot be made flat");

            if (Directory.Exists(entry)) Directory.Move(entry, target);
            else File.Move(entry, target);
        }

        Directory.Delete(projectDirectory);
    }
}
