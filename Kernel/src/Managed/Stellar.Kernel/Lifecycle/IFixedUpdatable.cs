using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// Takes part in the fixed steps of a main loop: physics, a deterministic simulation.
/// </summary>
/// <remarks>
/// Each frame, <see cref="FixedUpdate"/> is called once per fixed step the clock of the participant has run
/// since the previous frame (<see cref="Timing.IClock.EffectiveFixedStep"/>), with that step as the delta time. A
/// paused clock runs no steps. The number of steps per frame is limited, so that a slow frame does not make the next
/// one slower.
/// </remarks>
public interface IFixedUpdatable
    : IQuant
{
    /// <summary>
    /// Runs one fixed step.
    /// </summary>
    /// <param name="context">The frame.</param>
    void FixedUpdate(in FrameContext context);
}
