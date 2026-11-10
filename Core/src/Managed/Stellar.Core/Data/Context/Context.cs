using Stellar.Kernel.Data.Context;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Context;

/// <summary>
/// The plain stack-only context: a sender and nothing else.
/// </summary>
/// <param name="sender">The quant that initiated the call.</param>
public readonly ref struct Context(IQuantumObject? sender)
    : IContext
{
    /// <inheritdoc/>
    public IQuantumObject? Sender { get; } = sender;
}

/// <summary>
/// Stack-only context with typed data, passed by reference with no copies and no boxing.
/// </summary>
/// <typeparam name="TData">Type of the data.</typeparam>
public readonly ref struct Context<TData>
    : IContext<TData>
    where TData : struct, IContextData
{
    private readonly ref readonly TData _data;

    /// <summary>
    /// Creates a context. It must not outlive <paramref name="data"/>.
    /// </summary>
    /// <param name="sender">The quant that initiated the call.</param>
    /// <param name="data">The data of the operation.</param>
    public Context(IQuantumObject? sender, ref readonly TData data)
    {
        Sender = sender;
        _data = ref data;
    }

    /// <inheritdoc/>
    public IQuantumObject? Sender { get; }

    /// <inheritdoc/>
    public ref readonly TData Data => ref _data;
}
