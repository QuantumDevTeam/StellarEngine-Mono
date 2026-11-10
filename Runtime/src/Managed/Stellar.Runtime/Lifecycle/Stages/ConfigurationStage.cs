using Stellar.Runtime.Configuration;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Modules;

namespace Stellar.Runtime.Lifecycle.Stages;

/// <summary>
/// Reads the runtime configuration of the application from its assembly, without loading it: the settings of the
/// runtime and the modules to load.
/// </summary>
public sealed class ConfigurationStage
    : IRuntimeStage
{
    /// <inheritdoc/>
    public string Name => "Configuration";

    /// <inheritdoc/>
    public CatchLevel CatchLevel => CatchLevel.Runtime;

    /// <inheritdoc/>
    public ValueTask StartAsync(RuntimeContext context)
    {
        var configuration = context.ConfigurationLocator.Read(context.Launch.ApplicationPath);

        context.Configuration = configuration;
        context.Settings = RuntimeSettings.Create(configuration, context.Launch.Arguments);
        context.Log.Verbose |= context.Settings.Verbose;
        context.Plan = ModulePlan.From(configuration);

        context.Log.Step($"{configuration.ProjectName} {configuration.Version}: " +
                         $"{string.Join(", ", context.Plan.Modules.Select(module => module.Id))}");

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(RuntimeContext context)
    {
        return ValueTask.CompletedTask;
    }
}
