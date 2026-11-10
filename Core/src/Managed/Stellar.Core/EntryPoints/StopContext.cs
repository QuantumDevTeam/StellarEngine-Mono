using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.EntryPoints;

/// <inheritdoc/>
/// <param name="sender">The quant that requests the stop.</param>
/// <param name="reason">The reason for stopping.</param>
public readonly ref struct StopContext(IQuantumObject? sender, StopReason reason)
    : IStopContext
{
    /// <inheritdoc/>
    public IQuantumObject? Sender { get; } = sender;

    /// <inheritdoc/>
    public StopReason Reason { get; } = reason;
}
