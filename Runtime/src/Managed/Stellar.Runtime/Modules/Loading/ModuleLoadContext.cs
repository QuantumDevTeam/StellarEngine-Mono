using System.Reflection;
using System.Runtime.Loader;

namespace Stellar.Runtime.Modules.Loading;

/// <summary>
/// The context of one module: its assembly lives here, alone, so that the module can be unloaded. What it references
/// is found by the <see cref="ModuleContexts"/> it belongs to.
/// </summary>
public sealed class ModuleLoadContext
    : AssemblyLoadContext
{
    private readonly ModuleContexts _owner;

    internal ModuleLoadContext(string moduleId, bool isCollectible, ModuleContexts owner)
        : base($"Stellar.Module:{moduleId}", isCollectible)
    {
        ModuleId = moduleId;
        _owner = owner;
    }

    /// <summary>
    /// Gets the module of this context.
    /// </summary>
    public string ModuleId { get; }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        return _owner.Resolve(assemblyName);
    }

    /// <inheritdoc/>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        return _owner.Resolver.ResolveNativeLibrary(unmanagedDllName) is { } path
            ? LoadUnmanagedDllFromPath(path)
            : IntPtr.Zero;
    }
}
