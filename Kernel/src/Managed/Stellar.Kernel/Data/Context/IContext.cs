using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Data.Context;

/// <summary>
/// Represents an execution context for an operation inside the engine.
/// </summary>
/// <remarks>
/// <para>A context is the way one quant talks to a system: it carries the sender (initiator of the call)
/// and whatever the operation needs, and it lives only for the duration of that call.</para>
/// <para>Concrete contexts are usually <c>readonly ref struct</c> types, so APIs that accept a context are
/// generic and constrained with <c>allows ref struct</c>. This keeps them on the stack with no boxing.
/// Internal engine paths may skip contexts entirely and work with raw data.</para>
/// </remarks>
/// <example>
/// Accepting a context:
/// <code>
/// public override int Run&lt;TContext&gt;(ref readonly TContext context)
/// {
///     context.Logger?.Log(LogLevel.Info, "Module started");
///     return 0;
/// }
/// </code>
/// </example>
public interface IContext
    : IQuantumObject
{
    /// <summary>
    /// Quantum object that initiated this context's execution.
    /// </summary>
    /// <value>The sender, or <c>null</c> if the context was created by the system without an explicit sender.</value>
    IQuantumObject? Sender { get; }
}

/// <summary>
/// An <see cref="IContext"/> that carries operation-specific data.
/// </summary>
/// <typeparam name="TData">Type of the data. Must be a structure implementing <see cref="IContextData"/>.</typeparam>
public interface IContext<TData>
    : IContext
    where TData : struct, IContextData
{
    /// <summary>
    /// Operation-specific data, exposed by reference to avoid copies.
    /// </summary>
    ref readonly TData Data { get; }
}
