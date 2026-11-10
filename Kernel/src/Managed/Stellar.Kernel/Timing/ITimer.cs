using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Timing;

/// <summary>
/// Work scheduled on a clock: it runs on the thread of its main loop, at the start of the first frame at which its
/// clock has reached the due time. A paused clock holds its timers.
/// </summary>
public interface ITimer
    : IQuantumObject
{
    /// <summary>
    /// Gets the clock the timer follows.
    /// </summary>
    IClock Clock { get; }

    /// <summary>
    /// Gets the time of the clock at which the timer runs next.
    /// </summary>
    double DueTime { get; }

    /// <summary>
    /// Gets the period of a repeating timer, in seconds of its clock, or 0 for a timer that runs once.
    /// </summary>
    double Period { get; }

    /// <summary>
    /// Gets how many times the timer has run.
    /// </summary>
    ulong Runs { get; }

    /// <summary>
    /// Gets whether the timer will still run.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Cancels the timer. Safe from any thread.
    /// </summary>
    void Cancel();
}
