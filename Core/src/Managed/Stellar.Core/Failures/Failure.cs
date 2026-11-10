using Stellar.Kernel.Failures;

namespace Stellar.Core.Failures;

/// <inheritdoc/>
public readonly struct Failure
    : IFailure
{
    /// <inheritdoc/>
    public required string Message { get; init; }

    /// <inheritdoc/>
    public required Exception? InnerException { get; init; }

    /// <inheritdoc/>
    public required FailureType Type { get; init; }

    /// <inheritdoc/>
    public required IFailureLevel Level { get; init; }

    /// <inheritdoc/>
    public required string Source { get; init; }
}
