using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// A quant that takes part in the update phases of the main loop.
/// </summary>
public interface IUpdatable
    : IQuant
{
    /// <summary>
    /// Called before <see cref="Update{TContext}"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    void PreUpdate<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct
    {
    }

    /// <summary>
    /// The main update of the quant.
    /// </summary>
    /// <param name="context">The frame context.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    void Update<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct;

    /// <summary>
    /// Called after <see cref="Update{TContext}"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    void PostUpdate<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct
    {
    }
}
