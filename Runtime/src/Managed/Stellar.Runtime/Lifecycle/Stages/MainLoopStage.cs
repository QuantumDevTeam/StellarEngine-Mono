using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Lifecycle;
using Stellar.Runtime.Failures;

namespace Stellar.Runtime.Lifecycle.Stages;

/// <summary>
/// Runs the loops until the engine is asked to stop: the primary loop on the main thread, the dedicated ones on their
/// threads. With the <c>MainLoop</c> setting off, the engine stops as
/// soon as the application has started.
/// </summary>
public sealed class MainLoopStage
    : IRuntimeStage
{
    /// <inheritdoc/>
    public string Name => "Main loop";

    /// <inheritdoc/>
    public CatchLevel CatchLevel => CatchLevel.Engine;

    /// <inheritdoc/>
    public ValueTask StartAsync(RuntimeContext context)
    {
        if (!context.Settings.MainLoop || context.MainLoop is null)
        {
            context.Control.RequestStop(StopReason.Regular);
            return ValueTask.CompletedTask;
        }

        context.Control.SetState(EngineState.Running);
        context.Loops!.Start();
        context.MainLoop.Run();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(RuntimeContext context)
    {
        // The other loops stop with the primary one, before anything is taken down
        context.Loops?.Stop();
        return ValueTask.CompletedTask;
    }
}
