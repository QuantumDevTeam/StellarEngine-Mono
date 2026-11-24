using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stellar.Kernel.Configuration.Manifest;
using Stellar.Kernel.Modules;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Modules;

namespace Stellar.Tools.Solutions;

/// <summary>
/// The modules of a solution: the libraries of a game, found beside it and put into a catalog of their own, so that
/// the game names them as it names any module of the engine.
/// </summary>
/// <remarks>
/// <para>A module of a solution is a folder with a <c>&lt;Id&gt;.stellar.module</c> file, as everywhere: found below the
/// directory of the solution, or listed in <c>Modules</c> of the <c>.stellar.solution</c> file. It is a custom module,
/// level <see cref="ModuleLevel.EndPoint"/> (<c>StellarSdkUsage</c> <c>Module</c>), and depends only on modules of
/// lower levels, those of the installation included.</para>
/// <para>The catalog is <c>.stellar/stellar.modules.props</c> in the solution, the format of the catalog of the
/// installation. The SDK imports it after that one. Modules of a game are never packed, so they are always taken as
/// sources.</para>
/// </remarks>
public static class SolutionModules
{
    /// <summary>The folder of the generated files of a solution.</summary>
    public const string CatalogFolder = ".stellar";

    /// <summary>The file name of the catalog.</summary>
    public const string CatalogFileName = "stellar.modules.props";

    /// <summary>The version of a module whose descriptor has none.</summary>
    public const string DefaultVersion = "1.0.0";

    private const int SearchDepth = 3;

    /// <summary>
    /// Gets the path of the catalog of a solution.
    /// </summary>
    /// <param name="solutionDirectory">The directory of the solution.</param>
    /// <returns>The path; the file may not exist.</returns>
    public static string CatalogPath(string solutionDirectory)
    {
        return Path.Combine(solutionDirectory, CatalogFolder, CatalogFileName);
    }

    /// <summary>
    /// Finds the modules of a solution and checks them against the modules of the installation.
    /// </summary>
    /// <param name="solution">The solution.</param>
    /// <param name="installationModules">The modules of the installation, which the new ones may depend on.</param>
    /// <returns>The modules of the solution, sorted by identifier.</returns>
    /// <exception cref="ToolException">Thrown if a module breaks the rules.</exception>
    public static IReadOnlyList<ResolvedModule> Resolve(
        StellarSolution solution, IReadOnlyCollection<ResolvedModule> installationModules)
    {
        var errors = new List<string>();
        var files = new List<string>(ModuleDiscovery.Discover([solution.Directory], SearchDepth));

        foreach (var entry in solution.File.Modules.Where(entry => !string.IsNullOrWhiteSpace(entry)))
        {
            var directory = Path.GetFullPath(Path.Combine(solution.Directory, entry));
            var file = ModuleDescriptorFile.Find(directory);

            if (file is null)
                errors.Add($"{StellarSolution.Extension}: {entry} has no module descriptor ({directory})");
            else if (!files.Contains(file, StringComparer.OrdinalIgnoreCase))
                files.Add(file);
        }

        var modules = new List<ResolvedModule>();

        foreach (var file in files)
        {
            ModuleDescriptor descriptor;
            try
            {
                descriptor = ModuleResolver.Copy(ModuleDescriptorFile.Read(file));
            }
            catch (ToolException exception)
            {
                errors.Add(exception.Message);
                continue;
            }

            var directory = Path.GetDirectoryName(file)!;

            if (descriptor.Level != ModuleLevel.EndPoint)
            {
                errors.Add($"{descriptor.Id}: a module of a solution is a custom module, level {ModuleLevel.EndPoint}, " +
                           $"not {descriptor.Level} ({file})");
                continue;
            }

            descriptor.Version ??= DefaultVersion;
            ModuleResolver.ApplyConventions(descriptor, directory);

            // The core is implicit, as for the modules of the installation
            descriptor.Dependencies = descriptor.Dependencies
                .Where(dependency => !ModuleResolver.CoreModules.Contains(dependency)).ToArray();

            var module = new ResolvedModule(descriptor, directory, ModuleSource.Solution, ModuleLink.Sources);

            if (module.ProjectPath is not { } project)
                errors.Add($"{descriptor.Id}: no managed project found in {directory}");
            else if (!File.Exists(project))
                errors.Add($"{descriptor.Id}: the project {project} does not exist");

            modules.Add(module);
        }

        if (errors.Count == 0)
        {
            var graph = ModuleGraph.Analyze(installationModules.Concat(modules).Select(module => module.Descriptor));
            errors.AddRange(graph.Errors);
        }

        if (errors.Count != 0)
            throw new ToolException(
                $"The modules of {solution.Name} are inconsistent:\n  {string.Join("\n  ", errors)}");

        return modules.OrderBy(module => module.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Finds the modules of a solution and writes their catalog; with none, an old catalog is removed.
    /// </summary>
    /// <param name="solution">The solution.</param>
    /// <param name="installation">The installation the modules are built against.</param>
    /// <param name="logger">The output.</param>
    /// <returns>The modules of the solution.</returns>
    /// <exception cref="ToolException">Thrown if the modules of the installation or the solution break the rules.</exception>
    public static IReadOnlyList<ResolvedModule> Sync(
        StellarSolution solution, StellarInstallation installation, IToolLogger logger)
    {
        var modules = Resolve(solution, ModuleSynchronizer.Load(installation).LoadOrder);
        var path = CatalogPath(solution.Directory);

        if (modules.Count == 0)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                logger.Info($"No modules in {solution.Name}: {path} removed");
            }

            return modules;
        }

        var text = ModuleCatalogGenerator.Render(modules, "'stellar solution sync' from the module descriptors of the solution");
        if (FileWriter.WriteAllText(path, text))
            logger.Info($"Module catalog of the solution updated: {modules.Count} module(s), {path}");
        else
            logger.Verbose("Module catalog of the solution is up to date");

        return modules;
    }
}
