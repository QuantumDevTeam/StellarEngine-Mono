using Stellar.Kernel.Failures.Handlers;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Failures;

/// <summary>
/// The central component that receives failures and routes them to appropriate handlers.
/// </summary>
/// <remarks>
/// The dispatcher is a quant (<see cref="IQuant"/>) that owns the failure handling pipeline. It uses an
/// <see cref="IFailureHandlerProvider"/> to obtain handlers and invokes all of them.
/// </remarks>
public interface IFailureDispatcher
    : IQuant
{
    /// <summary>
    /// Dispatches the failure described in the context to registered handlers.
    /// </summary>
    /// <param name="failureContext">The context containing the failure.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    /// <returns><c>true</c> if at least one handler asked to stop execution; otherwise, <c>false</c>.</returns>
    bool Dispatch<TContext>(ref readonly TContext failureContext)
        where TContext : IFailureContext, allows ref struct;
}
