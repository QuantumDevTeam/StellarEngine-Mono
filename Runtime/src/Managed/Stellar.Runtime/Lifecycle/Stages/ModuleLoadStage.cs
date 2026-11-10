using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Modules;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Modules;
using Stellar.Runtime.Modules.Loading;

namespace Stellar.Runtime.Lifecycle.Stages;

/// <summary>
/// Loads the assemblies of the modules and of the application, each where it belongs, and finds their entry points.
/// Nothing runs yet.
/// </summary>
/// <remarks>
/// What the process already holds (the Kernel and the runtime, which the launcher brought) is taken as it is. Every
/// missing module is reported at once.
/// </remarks>
public sealed class ModuleLoadStage
    : IRuntimeStage
{
    /// <inheritdoc/>
    public string Name => "Module loading";

    /// <inheritdoc/>
    public CatchLevel CatchLevel => CatchLevel.Runtime;

    /// <inheritdoc/>
    public ValueTask StartAsync(RuntimeContext context)
    {
        var plan = context.Plan ??
                   throw new InvalidOperationException("The modules are loaded after the configuration");
        var configuration = context.Configuration!;

        var contexts = new ModuleContexts(new ApplicationAssemblyResolver(context.Launch.ApplicationPath),
            context.Settings.CollectibleModules, context.PlacementPolicy);
        context.Contexts = contexts;

        var application = new RuntimeModuleData
        {
            Id = string.IsNullOrEmpty(configuration.ProjectName)
                ? context.Launch.ApplicationName
                : configuration.ProjectName,
            Version = configuration.Version,
            Level = ModuleLevel.EndPoint,
            Assembly = context.Launch.ApplicationName,
            Dependencies = plan.Modules.Select(module => module.Id).ToArray()
        };

        foreach (var module in plan.Modules) contexts.Register(module);
        contexts.Register(application);

        var missing = new List<string>();

        foreach (var module in plan.Modules)
        {
            if (!ModuleContexts.IsLoadedInDefault(module.AssemblyName) &&
                contexts.Resolver.ResolveAssembly(new System.Reflection.AssemblyName(module.AssemblyName)) is null)
            {
                missing.Add(module.Id);
                continue;
            }

            var assembly = contexts.Load(module);
            context.Modules.Add(new LoadedModule(module, assembly, EntryPointLocator.Find(assembly)));
        }

        if (missing.Count > 0)
            throw new ModuleLoadException(
                $"Modules missing from {contexts.Resolver.Directory}: {string.Join(", ", missing)}");

        var applicationAssembly = contexts.Load(application);
        var entryPoint = EntryPointLocator.Find(applicationAssembly, configuration.EntryPoint)
                         ?? throw new ModuleLoadException($"The application {application.Id} has no entry point");

        context.Application = new LoadedModule(application, applicationAssembly, entryPoint, true);

        foreach (var module in context.Modules.Append(context.Application))
            context.Log.Step($"Loaded {module} into {module.LoadContext?.Name}");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(RuntimeContext context)
    {
        context.Application = null;
        context.Modules.Clear();

        if (context.Contexts is { } contexts)
        {
            contexts.Unload();
            contexts.Dispose();
            context.Contexts = null;
        }

        return ValueTask.CompletedTask;
    }
}
