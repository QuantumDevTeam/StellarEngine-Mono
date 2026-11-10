using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Lifecycle;
using Stellar.Runtime.Diagnostics;
using Stellar.Runtime.Failures;
using Stellar.Runtime.Lifecycle;
using Stellar.Runtime.Lifecycle.Stages;
using Stellar.Runtime.Threading;

namespace Stellar.Runtime.Hosting;

/// <summary>
/// Runs an application on the engine, on the calling thread: that thread becomes the main thread of the engine.
/// </summary>
/// <remarks>
/// The life of the engine is a list of stages (<see cref="DefaultStages"/>): configuration, module loading, engine
/// start, application start, main loop. A host can be given other stages, or the same ones with some replaced.
/// </remarks>
public sealed class RuntimeHost
    : IDisposable
{
    private readonly RuntimePipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeHost"/> class.
    /// </summary>
    /// <param name="launch">What to run.</param>
    /// <param name="stages">The stages; <see cref="DefaultStages"/> by default.</param>
    /// <param name="log">The log of the runtime; the error output by default.</param>
    public RuntimeHost(RuntimeLaunch launch, IEnumerable<IRuntimeStage>? stages = null, RuntimeLog? log = null)
    {
        Dispatcher = new MainThreadDispatcher();
        Control = new EngineControl();
        Context = new RuntimeContext(launch, log ?? new RuntimeLog(), Dispatcher, Control);
        _pipeline = new RuntimePipeline((stages ?? DefaultStages()).ToList());
    }

    /// <summary>
    /// Gets everything the stages share.
    /// </summary>
    public RuntimeContext Context { get; }

    /// <summary>
    /// Gets the control of the engine.
    /// </summary>
    public EngineControl Control { get; }

    /// <summary>
    /// Gets the dispatcher of the main thread.
    /// </summary>
    public MainThreadDispatcher Dispatcher { get; }

    /// <summary>
    /// The stages of the life of the engine, in order.
    /// </summary>
    /// <returns>New instances of the stages.</returns>
    public static IReadOnlyList<IRuntimeStage> DefaultStages()
    {
        return
        [
            new ConfigurationStage(),
            new ModuleLoadStage(),
            new EngineStartStage(),
            new ApplicationStartStage(),
            new MainLoopStage()
        ];
    }

    /// <summary>
    /// Runs the application until the engine stops.
    /// </summary>
    /// <returns>The exit code (<see cref="RuntimeExitCodes"/>).</returns>
    public int Run()
    {
        Console.CancelKeyPress += OnCancelKeyPress;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // Everything from here on belongs to this engine, on this thread and wherever the work flows from it
        using var scope = EngineScope.Enter(Context.Engine);

        try
        {
            var failed = MainThreadPump.Run(Dispatcher, Context.Catchers.Engine, () => _pipeline.RunAsync(Context));
            return ExitCode(failed);
        }
        catch (Exception exception)
        {
            Context.Log.Error($"The runtime failed: {exception}");
            return RuntimeExitCodes.StartupFailure;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Context.Engine?.Dispose();
        Context.Contexts?.Dispose();
        Control.Dispose();
        Dispatcher.Dispose();
    }

    private int ExitCode(IRuntimeStage? failed)
    {
        if (failed?.CatchLevel == CatchLevel.Runtime) return RuntimeExitCodes.StartupFailure;
        if (Context.RequestedExitCode is { } code) return code;

        return failed is not null || Control.StopReason == StopReason.CriticalError
            ? RuntimeExitCodes.Failure
            : RuntimeExitCodes.Success;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs arguments)
    {
        // The first Ctrl+C stops the engine properly, a second one kills the process
        if (Control.IsStopRequested) return;

        arguments.Cancel = true;
        Control.RequestStop(StopReason.Regular);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs arguments)
    {
        arguments.SetObserved();

        var result = Context.Catchers.MainLoop.Handle(arguments.Exception, "an unobserved task");
        if (result.MustStop) Control.RequestStop(arguments.Exception);
    }
}
