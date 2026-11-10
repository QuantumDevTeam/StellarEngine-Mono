using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Modules;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Loop;
using Stellar.Runtime.Modules;

namespace Stellar.Runtime.Lifecycle.Stages;

/// <summary>
/// Starts the engine: the engine instance is created with its clocks and loops, then the modules start level by
/// level, Stellar.Core first.
/// </summary>
/// <remarks>
/// Modules of one level never depend on each other: with <c>ModuleInitializationParallelism</c> above 1 those
/// above the core start side by side, on worker threads. By default every module starts on the main thread, in order.
/// The modules stop in the reverse order they started.
/// </remarks>
public sealed class EngineStartStage
    : IRuntimeStage
{
    /// <inheritdoc/>
    public string Name => "Engine start";

    /// <inheritdoc/>
    public CatchLevel CatchLevel => CatchLevel.Initialization;

    /// <inheritdoc/>
    public async ValueTask StartAsync(RuntimeContext context)
    {
        var settings = context.Settings;
        context.Time.EngineClock.FixedStep = settings.FixedTimeStep;

        context.Loops ??= new MainLoops(new MainLoopOptions { Name = "Main" }, context.Time, context.Dispatcher,
            context.Catchers.MainLoop, context.Control);
        context.Loops.ConfigurePrimary(new MainLoopOptions
        {
            TargetFrameRate = settings.TargetFrameRate,
            MaxDeltaTime = settings.MaxDeltaTime,
            MaxFixedSteps = settings.MaxFixedSteps
        });
        context.Loops.PrimaryLoop.MaxFrames = settings.MaxFrames;

        context.Engine ??= new EngineInstance(context.Launch.ApplicationName, context.Control, context.Time,
            context.Loops, context.Dispatcher, () => context.Services);

        var parallelism = context.Settings.ModuleInitializationParallelism;

        foreach (var level in context.Modules.GroupBy(module => module.Data.Level).OrderBy(group => group.Key))
        {
            if (context.Control.IsStopRequested) return;

            var modules = level.ToList();

            if (parallelism == 1 || modules.Count == 1 || level.Key <= ModuleLevel.Core)
            {
                foreach (var module in modules) ModuleStarter.Start(context, module);
                continue;
            }

            using var slots = new SemaphoreSlim(parallelism);

            await Task.WhenAll(modules.Select(async module =>
            {
                await slots.WaitAsync();

                try
                {
                    await Task.Run(() => ModuleStarter.Start(context, module));
                }
                finally
                {
                    slots.Release();
                }
            }));
        }
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(RuntimeContext context)
    {
        foreach (var module in context.Started.Where(module => !module.IsApplication).Reverse())
            ModuleStarter.Stop(context, module);

        context.Loops?.Stop();
        return ValueTask.CompletedTask;
    }
}
