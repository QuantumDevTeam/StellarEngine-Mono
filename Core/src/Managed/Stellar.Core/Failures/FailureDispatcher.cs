using Stellar.Core.Data.Collections;
using Stellar.Core.Failures.Handlers;
using Stellar.Core.Quantization;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Failures.Handlers;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Failures;

/// <summary>
/// Meta of a failure dispatcher: its failure levels and handlers.
/// </summary>
public interface IFailureDispatcherMeta
    : IMetaQuant
{
    /// <summary>
    /// Gets the failure levels known to the dispatcher.
    /// </summary>
    DataContainer<IFailureLevel> FailureLevels { get; }

    /// <summary>
    /// Gets the source of failure handlers.
    /// </summary>
    IFailureHandlerProvider FailureHandlerProvider { get; }
}

/// <inheritdoc cref="IFailureDispatcherMeta"/>
/// <param name="failureLevels">The container of failure levels. A new one is created when omitted.</param>
/// <param name="failureHandlerProvider">The source of handlers. A new one is created when omitted.</param>
public class FailureDispatcherMeta(
    DataContainer<IFailureLevel>? failureLevels = null,
    IFailureHandlerProvider? failureHandlerProvider = null
) : MetaQuant(Identifier.CreateAndRegister()), IFailureDispatcherMeta
{
    /// <inheritdoc/>
    public DataContainer<IFailureLevel> FailureLevels { get; } =
        failureLevels ?? new WritableTable<IFailureLevel>();

    /// <inheritdoc/>
    public IFailureHandlerProvider FailureHandlerProvider { get; } =
        failureHandlerProvider ?? new FailureHandlerProvider(new HandlerProviderMeta());
}

/// <inheritdoc cref="IFailureDispatcher"/>
/// <param name="meta">The meta with failure levels and handlers.</param>
public class FailureDispatcher(IFailureDispatcherMeta meta)
    : Quant<IFailureDispatcherMeta>(meta), IFailureDispatcher
{
    /// <inheritdoc/>
    public bool Dispatch<TContext>(ref readonly TContext failureContext)
        where TContext : IFailureContext, allows ref struct
    {
        var shouldStop = false;

        foreach (var handler in MetaQuant.FailureHandlerProvider.GetHandlers(failureContext.Failure))
            shouldStop |= handler.Handle(in failureContext);

        return shouldStop;
    }
}
