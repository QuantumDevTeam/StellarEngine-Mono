using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// A quant that takes part in the render phases of the main loop.
/// </summary>
public interface IRenderable
    : IQuant
{
    /// <summary>
    /// Called before <see cref="Render"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    void PreRender(in FrameContext context)
    {
    }

    /// <summary>
    /// The main render of the quant.
    /// </summary>
    /// <param name="context">The frame context.</param>
    void Render(in FrameContext context);

    /// <summary>
    /// Called after <see cref="Render"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    void PostRender(in FrameContext context)
    {
    }
}
