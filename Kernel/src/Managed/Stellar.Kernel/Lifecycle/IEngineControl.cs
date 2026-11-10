using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// Lets lower levels ask the runtime to stop, without knowing the runtime.
/// </summary>
/// <remarks>
/// Implemented by Stellar.RuntimeSystem and published through the service registry. Stellar.Core cannot reference
/// the runtime, so this contract is how a critical failure reaches it.
/// </remarks>
public interface IEngineControl
    : IQuantumObject
{
    /// <summary>
    /// Gets the current state of the engine.
    /// </summary>
    EngineState State { get; }

    /// <summary>
    /// Requests the engine to stop.
    /// </summary>
    /// <param name="reason">Why the engine should stop.</param>
    /// <param name="failure">The failure that caused the request, if any.</param>
    void RequestStop(StopReason reason, IFailure? failure = null);
}
