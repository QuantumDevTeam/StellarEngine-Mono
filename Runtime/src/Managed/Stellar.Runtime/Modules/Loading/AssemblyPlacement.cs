using Stellar.Kernel.Configuration.Modules;

namespace Stellar.Runtime.Modules.Loading;

/// <summary>
/// Where the assembly of a module lives.
/// </summary>
public enum AssemblyPlacement
{
    /// <summary>
    /// The default context, shared by everyone, never unloaded: the core of the engine, and what cannot live
    /// anywhere else.
    /// </summary>
    Default,

    /// <summary>
    /// A context of its own, which can be unloaded.
    /// </summary>
    Isolated
}

/// <summary>
/// Decides where the assembly of a module lives.
/// </summary>
public interface IAssemblyPlacementPolicy
{
    /// <summary>
    /// Decides where the assembly of a module lives.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="path">Its assembly.</param>
    /// <returns>The placement.</returns>
    AssemblyPlacement Place(RuntimeModuleData module, string path);
}

/// <summary>
/// The core of the engine and C++/CLI assemblies in the default context, every other module on its own.
/// </summary>
public sealed class DefaultAssemblyPlacementPolicy
    : IAssemblyPlacementPolicy
{
    /// <inheritdoc/>
    public AssemblyPlacement Place(RuntimeModuleData module, string path)
    {
        return ModulePlan.IsCoreLevel(module.Level) || AssemblyInspector.IsMixedMode(path)
            ? AssemblyPlacement.Default
            : AssemblyPlacement.Isolated;
    }
}
