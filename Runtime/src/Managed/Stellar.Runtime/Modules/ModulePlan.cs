using Stellar.Kernel.Configuration;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Modules;

namespace Stellar.Runtime.Modules;

/// <summary>
/// The modules of an application in the order they are started, as the build resolved them.
/// </summary>
public sealed class ModulePlan
{
    private readonly Dictionary<string, RuntimeModuleData> _byAssembly;

    private ModulePlan(IReadOnlyList<RuntimeModuleData> modules)
    {
        Modules = modules;
        _byAssembly = modules.ToDictionary(module => module.AssemblyName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the modules, every module after the ones it depends on.
    /// </summary>
    public IReadOnlyList<RuntimeModuleData> Modules { get; }

    /// <summary>
    /// Takes the plan from a runtime configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="ModuleLoadException">
    /// Thrown if the configuration has no modules, a module twice, or a module before one it depends on.
    /// </exception>
    public static ModulePlan From(RuntimeConfiguration configuration)
    {
        var component = configuration.Components.OfType<ModulesComponent>().FirstOrDefault()
                        ?? throw new ModuleLoadException(
                            $"The runtime configuration of {configuration.ProjectName} lists no modules: it was built by an " +
                            "older Stellar.Sdk, rebuild it");

        return Create(component.Modules);
    }

    /// <summary>
    /// Creates a plan.
    /// </summary>
    /// <param name="modules">The modules in load order.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="ModuleLoadException">
    /// Thrown if a module is there twice, or before one it depends on.
    /// </exception>
    public static ModulePlan Create(IEnumerable<RuntimeModuleData> modules)
    {
        var ordered = modules.ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var module in ordered)
        {
            if (!seen.Add(module.Id))
                throw new ModuleLoadException($"The module {module.Id} is listed twice");

            foreach (var dependency in module.Dependencies)
                if (!seen.Contains(dependency) && ordered.Any(other => other.Id == dependency))
                    throw new ModuleLoadException(
                        $"The module {module.Id} is listed before {dependency}, which it depends on");
        }

        return new ModulePlan(ordered);
    }

    /// <summary>
    /// Finds the module an assembly belongs to.
    /// </summary>
    /// <param name="assemblyName">The simple name of the assembly.</param>
    /// <returns>The module, or <c>null</c> if the assembly is not one.</returns>
    public RuntimeModuleData? FindByAssembly(string assemblyName)
    {
        return _byAssembly.GetValueOrDefault(assemblyName);
    }

    /// <summary>
    /// Groups the modules by level, lowest first: modules of one level never depend on each other.
    /// </summary>
    /// <returns>The groups, each in load order.</returns>
    public IEnumerable<IReadOnlyList<RuntimeModuleData>> ByLevel()
    {
        return Modules.GroupBy(module => module.Level)
            .OrderBy(group => group.Key)
            .Select(group => (IReadOnlyList<RuntimeModuleData>)group.ToList());
    }

    /// <summary>
    /// Gets whether a level is the core of the engine (the Kernel, Stellar.Core).
    /// </summary>
    /// <param name="level">The level.</param>
    public static bool IsCoreLevel(ModuleLevel level)
    {
        return level <= ModuleLevel.Core;
    }
}
