using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
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
    public void Create(string template, string projectName, string outputDirectory, StellarInstallation installation)
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
            ["StellarSchemasUri"] = new System.Uri(installation.Schemas + Path.DirectorySeparatorChar).AbsoluteUri
        });

        _logger.Info($"Project {projectName} created in {outputDirectory}");
    }
}
