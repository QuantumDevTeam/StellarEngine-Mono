using Stellar.Core.Failures;
using Stellar.Core.Global;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.FailureSystem;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Services;

namespace Stellar.Core;

/// <summary>
/// Entry point of the Core module: brings up the failure system every other module relies on.
/// </summary>
/// <remarks>
/// It owns the services of the engine (<see cref="IServiceHost"/>): the runtime publishes its own services there
/// (<see cref="IEngineControl"/>, the main loop) before it runs this entry point.
/// </remarks>
[StellarEntryPoint]
public sealed class EntryPoint(RuntimeConfiguration runtimeConfiguration)
    : StellarEntryPoint("S.C/MainEntry", runtimeConfiguration), IServiceHost
{
    private FailureSystem? _failures;
    private CoreEngine? _engine;

    /// <summary>
    /// Gets the core of the engine this entry point runs in: the one of the current engine
    /// (<see cref="Kernel.Lifecycle.EngineScope"/>), fixed the first time it is asked for.
    /// </summary>
    public CoreEngine Core => _engine ??= CoreEngine.Current;

    /// <inheritdoc/>
    public IServiceCatalog Services => Core.Services;

    /// <inheritdoc/>
    public override int Run<TContext>(ref readonly TContext context)
    {
        // The engine of the context wins: it is the one the runtime runs this entry point for
        if (context.Engine is { } instance) _engine ??= CoreEngine.For(instance);

        var core = Core;

        // The logger and the control of the engine may come after: they are looked up when a failure needs them
        _failures = new FailureSystem(() => core.Services.Find<ILogger>(), () => core.Services.Find<IEngineControl>());
        core.Services.Set<IFailureSystem>(_failures);

        return 0;
    }

    /// <inheritdoc/>
    public override void RequestStop<TContext>(ref readonly TContext context)
    {
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        if (_failures is null) return;

        var core = Core;
        core.Services.Remove<IFailureSystem>();
        _failures = null;

        // The engine is gone with its Core module: its registries, managed and native, go with it
        if (core.Instance is { } instance) CoreEngine.Release(instance);
    }
}
