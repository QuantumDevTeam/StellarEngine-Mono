using Stellar.Runtime.Failures;

namespace Stellar.Runtime.Lifecycle.Stages;

/// <summary>
/// Starts the application, once the engine is up.
/// </summary>
public sealed class ApplicationStartStage
    : IRuntimeStage
{
    /// <inheritdoc/>
    public string Name => "Application start";

    /// <inheritdoc/>
    public CatchLevel CatchLevel => CatchLevel.Initialization;

    /// <inheritdoc/>
    public ValueTask StartAsync(RuntimeContext context)
    {
        var application = context.Application
                          ?? throw new InvalidOperationException("The application starts after it is loaded");

        ModuleStarter.Start(context, application);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(RuntimeContext context)
    {
        if (context.Application is { } application && context.Started.Contains(application))
            ModuleStarter.Stop(context, application);

        return ValueTask.CompletedTask;
    }
}
