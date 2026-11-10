using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// A quant that takes part in the render phases of the main loop.
/// </summary>
public interface IRenderable
    : IQuant
{
    /// <summary>
    /// Called before <see cref="Render{TContext}"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    void PreRender<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct
    {
    }

    /// <summary>
    /// The main render of the quant.
    /// </summary>
    /// <param name="context">The frame context.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    void Render<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct;

    /// <summary>
    /// Called after <see cref="Render{TContext}"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    void PostRender<TContext>(ref readonly TContext context)
        where TContext : IFrameContext, allows ref struct
    {
    }
}
