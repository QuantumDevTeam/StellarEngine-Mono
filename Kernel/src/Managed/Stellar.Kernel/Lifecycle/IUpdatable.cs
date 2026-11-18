using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// A quant that takes part in the update phases of the main loop.
/// </summary>
public interface IUpdatable
    : IQuant
{
    /// <summary>
    /// Called before <see cref="Update"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    void PreUpdate(in FrameContext context)
    {
    }

    /// <summary>
    /// The main update of the quant.
    /// </summary>
    /// <param name="context">The frame context.</param>
    void Update(in FrameContext context);

    /// <summary>
    /// Called after <see cref="Update"/>. Does nothing by default.
    /// </summary>
    /// <param name="context">The frame context.</param>
    void PostUpdate(in FrameContext context)
    {
    }
}
