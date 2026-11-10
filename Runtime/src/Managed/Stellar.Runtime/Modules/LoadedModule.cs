using System.Reflection;
using System.Runtime.Loader;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.EntryPoint;

namespace Stellar.Runtime.Modules;

/// <summary>
/// A module the runtime has loaded.
/// </summary>
/// <param name="data">The module, as the build resolved it.</param>
/// <param name="assembly">Its assembly.</param>
/// <param name="entryPointType">Its entry point, if it has one.</param>
/// <param name="isApplication">Whether it is the application.</param>
public sealed class LoadedModule(
    RuntimeModuleData data,
    Assembly assembly,
    Type? entryPointType,
    bool isApplication = false)
{
    /// <summary>
    /// Gets the module, as the build resolved it.
    /// </summary>
    public RuntimeModuleData Data { get; } = data;

    /// <summary>
    /// Gets the identifier of the module.
    /// </summary>
    public string Id => Data.Id;

    /// <summary>
    /// Gets the assembly of the module.
    /// </summary>
    public Assembly Assembly { get; } = assembly;

    /// <summary>
    /// Gets the context the assembly is loaded in.
    /// </summary>
    public AssemblyLoadContext? LoadContext => AssemblyLoadContext.GetLoadContext(Assembly);

    /// <summary>
    /// Gets the type of the entry point, if the module has one.
    /// </summary>
    public Type? EntryPointType { get; } = entryPointType;

    /// <summary>
    /// Gets whether the module is the application.
    /// </summary>
    public bool IsApplication { get; } = isApplication;

    /// <summary>
    /// Gets or sets the running entry point.
    /// </summary>
    public StellarEntryPoint? EntryPoint { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Data.ToString();
    }
}
