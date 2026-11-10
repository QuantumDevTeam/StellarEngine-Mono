using Stellar.Kernel.Failures;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Failures;

/// <inheritdoc/>
/// <param name="sender">The quant that reports the failure.</param>
/// <param name="failure">The failure.</param>
public readonly ref struct FailureContext(IQuantumObject? sender, IFailure failure)
    : IFailureContext
{
    /// <inheritdoc/>
    public IQuantumObject? Sender { get; } = sender;

    /// <inheritdoc/>
    public IFailure Failure { get; } = failure;
}
