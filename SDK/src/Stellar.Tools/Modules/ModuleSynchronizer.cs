using System.Collections.Generic;
using System.Linq;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Manifests;

namespace Stellar.Tools.Modules;

/// <summary>
/// The modules of an installation, checked and in load order.
/// </summary>
/// <param name="Resolution">The modules and the problems found while putting them together.</param>
/// <param name="LoadOrder">The enabled modules, dependencies first.</param>
public sealed record ModuleSet(ModuleResolution Resolution, IReadOnlyList<ResolvedModule> LoadOrder);

/// <summary>
/// Brings the generated files of an installation in line with its modules: resolve, check the architecture rules,
/// write the catalog.
/// </summary>
public static class ModuleSynchronizer
{
    /// <summary>
    /// Resolves and checks the modules without writing anything.
    /// </summary>
    /// <param name="installation">The installation.</param>
    /// <returns>The modules in load order.</returns>
    /// <exception cref="ToolException">Thrown if the modules break the rules.</exception>
    public static ModuleSet Load(StellarInstallation installation)
    {
        var manifest = EngineManifestStore.Load(installation.EngineManifest);
        var resolution = ModuleResolver.Resolve(installation, manifest);

        if (!resolution.IsValid)
            throw Failure(resolution.Errors);

        var graph = ModuleGraph.Analyze(resolution.Modules.Select(module => module.Descriptor));
        if (!graph.IsValid)
            throw Failure(graph.Errors);

        var byId = resolution.Modules.ToDictionary(module => module.Id);
        return new ModuleSet(resolution, graph.LoadOrder.Select(descriptor => byId[descriptor.Id]).ToList());
    }

    /// <summary>
    /// Resolves and checks the modules, and writes the catalog and the lock file.
    /// </summary>
    /// <param name="installation">The installation.</param>
    /// <param name="logger">The output.</param>
    /// <returns>The modules in load order.</returns>
    /// <exception cref="ToolException">Thrown if the modules break the rules. Nothing is written then.</exception>
    public static ModuleSet Synchronize(StellarInstallation installation, IToolLogger logger)
    {
        var set = Load(installation);

        foreach (var warning in set.Resolution.Warnings)
            logger.Warning(warning);

        var catalogChanged =
            FileWriter.WriteAllText(installation.ModuleCatalog, ModuleCatalogGenerator.Render(set.LoadOrder));
        var lockChanged = FileWriter.WriteAllText(installation.ModuleLock,
            StellarJson.Serialize(new ModuleLockFile(set.LoadOrder)));

        if (catalogChanged || lockChanged)
            logger.Info($"Module catalog updated: {set.LoadOrder.Count} module(s), {installation.ModuleCatalog}");
        else
            logger.Verbose("Module catalog is up to date");

        return set;
    }

    private static ToolException Failure(IEnumerable<string> errors)
    {
        return new ToolException($"The modules of the installation are inconsistent:\n  {string.Join("\n  ", errors)}");
    }
}

/// <summary>
/// The content of <c>stellar.modules.json</c>: the resolved modules in load order, for tools that read JSON
/// (the editor, the runtime of a Debug build).
/// </summary>
public sealed class ModuleLockFile
{
    /// <summary>
    /// Creates the content.
    /// </summary>
    public ModuleLockFile(IEnumerable<ResolvedModule> loadOrder)
    {
        Modules = loadOrder.Select(module => new Entry
        {
            Id = module.Id,
            Version = module.Descriptor.Version ?? string.Empty,
            Level = module.Descriptor.Level.ToString(),
            Source = module.Source.ToString(),
            Link = module.Link.ToString(),
            Directory = module.Directory,
            Project = module.ProjectPath,
            NativeProjects = module.NativeProjectPaths,
            Dependencies = module.Descriptor.Dependencies
        }).ToArray();
    }

    /// <summary>Gets the modules in load order.</summary>
    public Entry[] Modules { get; }

    /// <summary>One module.</summary>
    public sealed class Entry
    {
        /// <summary>Gets the identifier.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Gets the version.</summary>
        public string Version { get; init; } = string.Empty;

        /// <summary>Gets the level.</summary>
        public string Level { get; init; } = string.Empty;

        /// <summary>Gets where the description came from.</summary>
        public string Source { get; init; } = string.Empty;

        /// <summary>Gets how projects reference the module.</summary>
        public string Link { get; init; } = string.Empty;

        /// <summary>Gets the module folder.</summary>
        public string? Directory { get; init; }

        /// <summary>Gets the managed project.</summary>
        public string? Project { get; init; }

        /// <summary>Gets the native projects.</summary>
        public string[] NativeProjects { get; init; } = new string[0];

        /// <summary>Gets the dependencies.</summary>
        public string[] Dependencies { get; init; } = new string[0];
    }
}
