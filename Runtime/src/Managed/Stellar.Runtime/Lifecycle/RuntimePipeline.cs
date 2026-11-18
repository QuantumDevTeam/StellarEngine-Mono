using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Profiling;

namespace Stellar.Runtime.Lifecycle;

/// <summary>
/// Runs the stages: starts them in order until one fails or the engine is asked to stop, then stops every stage
/// that was started, in the reverse order.
/// </summary>
/// <param name="stages">The stages.</param>
public sealed class RuntimePipeline(IReadOnlyList<IRuntimeStage> stages)
{
    /// <summary>
    /// Gets the stages.
    /// </summary>
    public IReadOnlyList<IRuntimeStage> Stages { get; } = stages;

    /// <summary>
    /// Runs the stages.
    /// </summary>
    /// <param name="context">The runtime.</param>
    /// <returns>The stage that failed to start, or <c>null</c> if none did.</returns>
    public async Task<IRuntimeStage?> RunAsync(RuntimeContext context)
    {
        var entered = new Stack<IRuntimeStage>();
        IRuntimeStage? failed = null;

        context.Control.SetState(EngineState.Initializing);

        foreach (var stage in Stages)
        {
            if (context.Control.IsStopRequested) break;

            context.Log.Step($"Start: {stage.Name}");
            entered.Push(stage);

            // A stage is a span on the timeline of a profiler: the stage of the main loop spans the whole run
            Failure? failure;
            using (new ProfilerMarker($"Runtime.{stage.Name}", ProfilerCategory.Modules).Auto())
                failure = await context.Catchers[stage.CatchLevel].GuardAsync(
                    () => stage.StartAsync(context), $"the stage {stage.Name}");

            if (failure is null) continue;

            failed = stage;
            context.Control.RequestStop(failure);
            break;
        }

        context.Control.SetState(EngineState.Stopping);

        while (entered.TryPop(out var stage))
        {
            context.Log.Step($"Stop: {stage.Name}");
            await context.Catchers.Engine.GuardAsync(() => stage.StopAsync(context), $"the stop of {stage.Name}");
        }

        context.Control.SetState(EngineState.Stopped);
        return failed;
    }
}
