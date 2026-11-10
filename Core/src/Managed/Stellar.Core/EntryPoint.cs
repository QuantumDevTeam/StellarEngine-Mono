using Stellar.Core.Failures;
using Stellar.Core.Failures.Handlers;
using Stellar.Core.Global;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Services;

namespace Stellar.Core;

/// <summary>
/// Entry point of the Core module: brings up the failure pipeline every other module relies on.
/// </summary>
/// <remarks>
/// It owns the services of the engine (<see cref="IServiceHost"/>): the runtime publishes its own services there
/// (<see cref="IEngineControl"/>, the main loop) before it runs this entry point.
/// </remarks>
[StellarEntryPoint]
public sealed class EntryPoint(RuntimeConfiguration runtimeConfiguration)
    : StellarEntryPoint("S.C/MainEntry", runtimeConfiguration), IServiceHost
{
    private FailureDispatcher? _dispatcher;
    private CoreEngine? _engine;

    /// <summary>
    /// Gets the core of the engine this entry point runs in: the one of the current engine
    /// (<see cref="Kernel.Lifecycle.EngineScope"/>), fixed the first time it is asked for.
    /// </summary>
    public CoreEngine Core => _engine ??= CoreEngine.Current;

    /// <inheritdoc/>
    public IServiceRegistry Services => Core.Services;

    /// <inheritdoc/>
    public override int Run<TContext>(ref readonly TContext context)
    {
        // The engine of the context wins: it is the one the runtime runs this entry point for
        if (context.Engine is { } instance) _engine ??= CoreEngine.For(instance);

        var core = Core;
        var meta = new FailureDispatcherMeta();
        core.FailureLevels.Initialize(meta);

        var provider = (FailureHandlerProvider)meta.FailureHandlerProvider;
        provider.RegisterDefaultHandler(new FailureHandler(context.Logger, core.Services.Find<IEngineControl>()));

        _dispatcher = new FailureDispatcher(meta);
        core.Services.Set<IFailureDispatcher>(_dispatcher);
        core.Services.Set<IFailureReporter>(
            new FailureReporter(_dispatcher, core.FailureLevels.Error!, core.FailureLevels.CriticalError!));

        return 0;
    }

    /// <inheritdoc/>
    public override void RequestStop<TContext>(ref readonly TContext context)
    {
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (_dispatcher is null) return;

        var core = Core;
        core.Services.Remove<IFailureReporter>();
        core.Services.Remove<IFailureDispatcher>();
        core.FailureLevels.Reset();
        _dispatcher = null;

        // The engine is gone with its Core module: its registries, managed and native, go with it
        if (core.Instance is { } instance) CoreEngine.Release(instance);
    }
}
