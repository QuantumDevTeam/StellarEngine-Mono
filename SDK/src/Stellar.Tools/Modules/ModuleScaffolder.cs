using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Stellar.Kernel.Modules;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Templates;

namespace Stellar.Tools.Modules;

/// <summary>
/// What a new module is made of.
/// </summary>
/// <param name="Id">The identifier, e.g. <c>Stellar.Logging</c>.</param>
/// <param name="Level">The architectural level.</param>
/// <param name="Description">What it is for, in one sentence.</param>
/// <param name="Template">The template: a folder of <c>Shared/Templates/Modules</c>.</param>
/// <param name="Directory">Where to create it. <c>modules/&lt;Id&gt;</c> of the installation when omitted.</param>
public sealed record ModuleRequest(
    string Id,
    ModuleLevel Level,
    string? Description,
    string Template,
    string? Directory = null);

/// <summary>
/// Creates modules from <c>Shared/Templates/Modules</c>, and registers them with the installation.
/// </summary>
public sealed class ModuleScaffolder
{
    private static readonly Regex IdPattern = new("^[A-Z][A-Za-z0-9]*(\\.[A-Z][A-Za-z0-9]*)+$", RegexOptions.Compiled);

    private readonly StellarInstallation _installation;
    private readonly TemplateRenderer _renderer;
    private readonly IToolLogger _logger;

    /// <summary>
    /// Creates a scaffolder.
    /// </summary>
    public ModuleScaffolder(StellarInstallation installation, IToolLogger logger)
    {
        _installation = installation;
        _renderer = new TemplateRenderer(logger);
        _logger = logger;
    }

    /// <summary>Gets the folder of module templates.</summary>
    public string TemplatesDirectory => Path.Combine(_installation.Templates, "Modules");

    /// <summary>
    /// Lists the available templates.
    /// </summary>
    public IReadOnlyList<string> ListTemplates()
    {
        return TemplateRenderer.List(TemplatesDirectory);
    }

    /// <summary>
    /// Gets what a template is for.
    /// </summary>
    public string? DescribeTemplate(string template)
    {
        return TemplateRenderer.ReadManifest(Path.Combine(TemplatesDirectory, template)).Description;
    }

    /// <summary>
    /// Creates a module. A module outside the searched folders is added to the manifest, so the installation
    /// finds it either way.
    /// </summary>
    /// <param name="request">What to create.</param>
    /// <returns>The module folder.</returns>
    /// <exception cref="ToolException">Thrown if the identifier is invalid, the template unknown or the folder taken.</exception>
    public string Create(ModuleRequest request)
    {
        if (!IdPattern.IsMatch(request.Id))
            throw new ToolException(
                $"'{request.Id}' is not a valid module identifier: use dotted PascalCase, e.g. Stellar.Logging");

        if (Array.IndexOf(ModuleResolver.CoreModules, request.Id) >= 0)
            throw new ToolException($"{request.Id} is part of the core");

        var template = Path.Combine(TemplatesDirectory, request.Template);
        if (!Directory.Exists(template))
            throw new ToolException(
                $"Unknown module template '{request.Template}'. Available: {string.Join(", ", ListTemplates())}");

        var directory = Path.GetFullPath(request.Directory ?? Path.Combine(_installation.Modules, request.Id));

        _renderer.Render(template, directory, Values(request));
        ModuleFeed.EnsureReachable(_installation, directory, _logger);
        _logger.Info($"Module {request.Id} ({request.Level}) created in {directory}");

        return directory;
    }

    /// <summary>
    /// Gets the placeholder values of a module.
    /// </summary>
    public Dictionary<string, string> Values(ModuleRequest request)
    {
        var shortName = request.Id.StartsWith("Stellar.", StringComparison.Ordinal)
            ? request.Id.Substring("Stellar.".Length)
            : request.Id;
        var description = request.Description ?? $"The {shortName} module of StellarEngine";

        return new Dictionary<string, string>
        {
            ["ModuleId"] = request.Id,
            ["ModuleName"] = shortName,
            ["ModuleIdentifier"] = shortName.Replace(".", string.Empty),
            ["ModuleNativeId"] = "Stellar.Native." + shortName,
            ["ModuleLevel"] = request.Level.ToString(),
            ["ModuleVersion"] = StellarVersions.Engine,
            // The description lands in JSON: it is escaped for it
            ["ModuleDescription"] = description.Replace("\\", "\\\\").Replace("\"", "\\\""),
            ["ModuleNativeGuid"] = Guid.NewGuid().ToString("B").ToUpperInvariant(),
            ["ModuleTestsNativeGuid"] = Guid.NewGuid().ToString("B").ToUpperInvariant(),
            ["SdkVersion"] = StellarVersions.Sdk,
            ["OrchesterVersion"] = StellarVersions.Orchester,
            ["StellarRoot"] = _installation.Root,
            ["StellarSchemasUri"] = new Uri(_installation.Schemas + Path.DirectorySeparatorChar).AbsoluteUri
        };
    }
}
