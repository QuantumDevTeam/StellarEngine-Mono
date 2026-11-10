using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Timing;

namespace Stellar.Runtime.Loop;

/// <summary>
/// What a participant of a main loop gets in a phase.
/// </summary>
/// <param name="loop">The loop.</param>
/// <param name="phase">The phase.</param>
/// <param name="frameIndex">The frame.</param>
/// <param name="clock">The clock of the participant.</param>
/// <param name="time">The time of that clock at this frame.</param>
/// <param name="deltaTime">The step of that clock.</param>
/// <param name="unscaledDeltaTime">The real time since the previous frame.</param>
public readonly ref struct FrameContext(
    IMainLoop loop,
    FramePhase phase,
    ulong frameIndex,
    IClock clock,
    double time,
    double deltaTime,
    double unscaledDeltaTime
) : IFrameContext
{
    /// <inheritdoc/>
    public IQuantumObject? Sender => Loop;

    /// <inheritdoc/>
    public IMainLoop Loop { get; } = loop;

    /// <inheritdoc/>
    public FramePhase Phase { get; } = phase;

    /// <inheritdoc/>
    public ulong FrameIndex { get; } = frameIndex;

    /// <inheritdoc/>
    public IClock Clock { get; } = clock;

    /// <inheritdoc/>
    public double Time { get; } = time;

    /// <inheritdoc/>
    public double DeltaTime { get; } = deltaTime;

    /// <inheritdoc/>
    public double UnscaledDeltaTime { get; } = unscaledDeltaTime;
}
