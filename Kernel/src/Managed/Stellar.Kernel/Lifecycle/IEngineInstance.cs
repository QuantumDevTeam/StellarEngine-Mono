using Stellar.Kernel.Quantization;
using Stellar.Kernel.Services;
using Stellar.Kernel.Threading;
using Stellar.Kernel.Timing;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// One engine: its control, its time, its loops, its services.
/// </summary>
/// <remarks>
/// <para>An engine is an object, not the process: several can live side by side (an editor and the game it runs),
/// each started, paused, stopped and failing on its own. A module reaches the engine it runs in through
/// <see cref="EntryPoint.IRunContext.Engine"/>, never through something global.</para>
/// </remarks>
public interface IEngineInstance
    : IQuantumObject
{
    /// <summary>
    /// Gets the name of the engine, unique within the process.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the control of the engine: its state, and the way to stop it.
    /// </summary>
    IEngineControl Control { get; }

    /// <summary>
    /// Gets the clocks of the engine.
    /// </summary>
    ITimeSystem Time { get; }

    /// <summary>
    /// Gets the loops of the engine.
    /// </summary>
    IMainLoops Loops { get; }

    /// <summary>
    /// Gets the dispatcher of the main thread of the engine.
    /// </summary>
    IMainThreadDispatcher Dispatcher { get; }

    /// <summary>
    /// Gets the services of the engine, once the module that owns them has started.
    /// </summary>
    IServiceRegistry? Services { get; }
}
