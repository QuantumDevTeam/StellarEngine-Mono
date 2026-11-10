using Stellar.Kernel.Failures;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Failures;

/// <summary>
/// Reports exceptions caught outside the failure system (by the runtime, which starts before Stellar.Core) as
/// failures: <see cref="Global.Engine.FailureLevels.Error"/>, or <see cref="Global.Engine.FailureLevels.CriticalError"/>
/// for the critical ones, through the dispatcher.
/// </summary>
/// <param name="dispatcher">The dispatcher.</param>
/// <param name="error">The level of failures.</param>
/// <param name="criticalError">The level of critical failures.</param>
public sealed class FailureReporter(IFailureDispatcher dispatcher, IFailureLevel error, IFailureLevel criticalError)
    : IFailureReporter
{
    /// <inheritdoc/>
    public bool Report(Exception exception, FailureType type, bool critical, IQuantumObject? sender = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var failure = new Failure
        {
            Message = exception.Message,
            InnerException = exception,
            Type = type,
            Level = critical ? criticalError : error,
            Source = exception.Source ?? string.Empty
        };

        var context = new FailureContext(sender, failure);
        return dispatcher.Dispatch(in context);
    }
}
