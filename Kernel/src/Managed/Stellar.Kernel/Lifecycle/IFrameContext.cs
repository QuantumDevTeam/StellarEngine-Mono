using Stellar.Kernel.Data.Context;
using Stellar.Kernel.Timing;

namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// What a participant of a main loop gets in a phase.
/// </summary>
/// <remarks>
/// The times are those of the clock of the participant (the one it was added with): a participant on a paused or
/// slowed clock gets what that clock gives, the others are not affected.
/// </remarks>
public interface IFrameContext
    : IContext
{
    /// <summary>
    /// Gets the phase being executed.
    /// </summary>
    FramePhase Phase { get; }

    /// <summary>
    /// Gets the number of the current frame of the loop.
    /// </summary>
    ulong FrameIndex { get; }

    /// <summary>
    /// Gets the time the clock of the participant advanced since the previous frame, in its seconds. In
    /// <see cref="FramePhase.FixedUpdate"/>, the fixed step.
    /// </summary>
    double DeltaTime { get; }

    /// <summary>
    /// Gets the real time since the previous frame of the loop, in seconds: no scale, no pause.
    /// </summary>
    double UnscaledDeltaTime { get; }

    /// <summary>
    /// Gets the time of the clock of the participant at this frame.
    /// </summary>
    double Time { get; }

    /// <summary>
    /// Gets the clock of the participant.
    /// </summary>
    IClock Clock { get; }

    /// <summary>
    /// Gets the loop running the frame.
    /// </summary>
    IMainLoop Loop { get; }
}
