using Stellar.Core.Quantization;
using Stellar.Kernel.EntryPoint;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Failures.Handlers;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.LoggingSystem;

namespace Stellar.Core.Failures.Handlers;

/// <summary>
/// The default handler: applies what the failure level says - logging, stopping, terminating.
/// </summary>
/// <param name="logger">The logger for loggable levels.</param>
/// <param name="engineControl">Receives the stop request of terminating levels.</param>
public class FailureHandler(ILogger? logger = null, IEngineControl? engineControl = null)
    : RegistrableQuant<FailureHandler, MetaQuant>(new MetaQuant()), IFailureHandler
{
    /// <inheritdoc/>
    public ILogger? Logger { get; set; } = logger;

    /// <inheritdoc/>
    public bool Handle<TContext>(ref readonly TContext context)
        where TContext : IFailureContext, allows ref struct
    {
        var failure = context.Failure;
        var level = failure.Level;

        if (!level.IsEnabled) return false;

        if (level.IsLoggable)
            Logger?.Log(LogLevel.Exception, failure.Message);

        if (level.ShouldTerminate)
            engineControl?.RequestStop(StopReason.CriticalError, failure);

        return level.IsStopExecute;
    }
}
