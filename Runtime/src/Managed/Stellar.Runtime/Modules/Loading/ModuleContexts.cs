using System.Reflection;
using System.Runtime.Loader;
using Stellar.Kernel.Configuration.Modules;

namespace Stellar.Runtime.Modules.Loading;

/// <summary>
/// Where every assembly of an application is loaded, and who finds what it references.
/// </summary>
/// <remarks>
/// <para>The default context holds the host (the launcher, the runtime, the Kernel), the core of the engine, C++/CLI
/// assemblies and the libraries the modules use: all of it shared, one copy each. Every other module gets a
/// <see cref="ModuleLoadContext"/> of its own.</para>
/// <para>A reference to a module always lands in the context of that module, whoever references it: there is one
/// copy of each module, and its types are the same for everyone.</para>
/// </remarks>
public sealed class ModuleContexts
    : IDisposable
{
    private readonly object _gate = new();
    private readonly IAssemblyPlacementPolicy _placement;
    private readonly Dictionary<string, RuntimeModuleData> _modules = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ModuleLoadContext> _contexts = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleContexts"/> class.
    /// </summary>
    /// <param name="resolver">Finds the files of the application.</param>
    /// <param name="collectible">Whether the contexts of modules can be unloaded.</param>
    /// <param name="placement">Decides where modules live; the core in the default context by default.</param>
    public ModuleContexts(ApplicationAssemblyResolver resolver, bool collectible = true,
        IAssemblyPlacementPolicy? placement = null)
    {
        Resolver = resolver;
        IsCollectible = collectible;
        _placement = placement ?? new DefaultAssemblyPlacementPolicy();

        AssemblyLoadContext.Default.Resolving += OnDefaultResolving;
        AssemblyLoadContext.Default.ResolvingUnmanagedDll += OnDefaultResolvingUnmanagedDll;
    }

    /// <summary>
    /// Gets what finds the files of the application.
    /// </summary>
    public ApplicationAssemblyResolver Resolver { get; }

    /// <summary>
    /// Gets whether the contexts of modules can be unloaded.
    /// </summary>
    public bool IsCollectible { get; }

    /// <summary>
    /// Gets the contexts of the modules loaded so far.
    /// </summary>
    public IReadOnlyCollection<ModuleLoadContext> Contexts
    {
        get
        {
            lock (_gate)
            {
                return _contexts.Values.ToList();
            }
        }
    }

    /// <summary>
    /// Makes a module known, so that references to its assembly land in its context.
    /// </summary>
    /// <param name="module">The module.</param>
    public void Register(RuntimeModuleData module)
    {
        lock (_gate)
        {
            _modules[module.AssemblyName] = module;
        }
    }

    /// <summary>
    /// Gets whether an assembly is loaded in the default context already (the launcher, the runtime, the Kernel, and
    /// what was placed there): it is shared as it is.
    /// </summary>
    /// <param name="assemblyName">The simple name of the assembly.</param>
    public static bool IsLoadedInDefault(string assemblyName)
    {
        return FindInDefault(assemblyName) is not null;
    }

    /// <summary>
    /// Loads the assembly of a module.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>The assembly.</returns>
    /// <exception cref="ModuleLoadException">Thrown if the assembly is not in the application.</exception>
    public Assembly Load(RuntimeModuleData module)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _modules[module.AssemblyName] = module;

            if (FindInDefault(module.AssemblyName) is { } shared) return shared;

            if (_contexts.TryGetValue(module.Id, out var existing) &&
                existing.Assemblies.FirstOrDefault(assembly => assembly.GetName().Name == module.AssemblyName) is
                    { } loaded)
                return loaded;

            var path = Resolver.ResolveAssembly(new AssemblyName(module.AssemblyName))
                       ?? throw new ModuleLoadException(
                           $"The assembly {module.AssemblyName} of the module {module.Id} is not in {Resolver.Directory}");

            if (_placement.Place(module, path) == AssemblyPlacement.Default)
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

            if (existing is null)
            {
                existing = new ModuleLoadContext(module.Id, IsCollectible, this);
                _contexts[module.Id] = existing;
            }

            return existing.LoadFromAssemblyPath(path);
        }
    }

    /// <summary>
    /// Unloads the contexts of the modules that can be unloaded. Their assemblies go away once nothing uses them.
    /// </summary>
    public void Unload()
    {
        List<ModuleLoadContext> contexts;

        lock (_gate)
        {
            contexts = _contexts.Values.Where(context => context.IsCollectible).ToList();
            foreach (var context in contexts) _contexts.Remove(context.ModuleId);
        }

        foreach (var context in contexts) context.Unload();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
        }

        AssemblyLoadContext.Default.Resolving -= OnDefaultResolving;
        AssemblyLoadContext.Default.ResolvingUnmanagedDll -= OnDefaultResolvingUnmanagedDll;
    }

    /// <summary>
    /// Finds a reference made from a module context.
    /// </summary>
    internal Assembly? Resolve(AssemblyName name)
    {
        if (name.Name is null || FindInDefault(name.Name) is not null) return null;

        RuntimeModuleData? module;
        lock (_gate)
        {
            module = _modules.GetValueOrDefault(name.Name);
        }

        if (module is not null) return Load(module);

        // A library: shared by every module that uses it
        return Resolver.ResolveAssembly(name) is { } path ? LoadShared(name.Name, path) : null;
    }

    private Assembly? OnDefaultResolving(AssemblyLoadContext context, AssemblyName name)
    {
        if (name.Name is null) return null;

        RuntimeModuleData? module;
        lock (_gate)
        {
            module = _modules.GetValueOrDefault(name.Name);
        }

        if (module is not null) return Load(module);

        return Resolver.ResolveAssembly(name) is { } path ? LoadShared(name.Name, path) : null;
    }

    private IntPtr OnDefaultResolvingUnmanagedDll(Assembly assembly, string name)
    {
        return Resolver.ResolveNativeLibrary(name) is { } path
            ? System.Runtime.InteropServices.NativeLibrary.Load(path)
            : IntPtr.Zero;
    }

    private Assembly LoadShared(string name, string path)
    {
        lock (_gate)
        {
            return FindInDefault(name) ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }
    }

    private static Assembly? FindInDefault(string name)
    {
        return AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly =>
            string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
