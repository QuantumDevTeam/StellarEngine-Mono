using Stellar.Kernel.LoggingSystem;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Failures.Handlers;

/// <summary>
/// Handles a specific failure by implementing custom logic.
/// </summary>
/// <remarks>
/// A failure handler is a registrable quant (<see cref="IRegistrableQuant"/>) that processes failures of certain
/// types or levels. Handlers are obtained from <see cref="IFailureHandlerProvider"/> and invoked by
/// <see cref="IFailureDispatcher"/>.
/// </remarks>
public interface IFailureHandler
    : IRegistrableQuant
{
    /// <summary>
    /// Gets the logger instance associated with this handler, or <c>null</c> if logging is unavailable.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>
    /// Handles the failure described in the context.
    /// </summary>
    /// <param name="context">The failure context containing the failure data.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    /// <returns><c>true</c> if the execution should stop after handling; otherwise, <c>false</c>.</returns>
    bool Handle<TContext>(ref readonly TContext context)
        where TContext : IFailureContext, allows ref struct;
}
