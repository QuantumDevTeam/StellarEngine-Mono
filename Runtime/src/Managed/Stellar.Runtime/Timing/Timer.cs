using Stellar.Kernel.Timing;
using ITimer = Stellar.Kernel.Timing.ITimer;

namespace Stellar.Runtime.Timing;

/// <summary>
/// Work scheduled on a clock by a main loop.
/// </summary>
public sealed class Timer
    : ITimer
{
    private long _runs;
    private volatile bool _active = true;

    internal Timer(IClock clock, double dueTime, double period, Action callback)
    {
        if (period < 0 || double.IsNaN(period)) throw new ArgumentOutOfRangeException(nameof(period));

        Clock = clock;
        DueTime = dueTime;
        Period = period;
        Callback = callback;
    }

    /// <inheritdoc/>
    public IClock Clock { get; }

    /// <inheritdoc/>
    public double DueTime { get; private set; }

    /// <inheritdoc/>
    public double Period { get; }

    /// <inheritdoc/>
    public ulong Runs => (ulong)Interlocked.Read(ref _runs);

    /// <inheritdoc/>
    public bool IsActive => _active;

    internal Action Callback { get; }

    /// <inheritdoc/>
    public void Cancel()
    {
        _active = false;
    }

    /// <summary>
    /// Records a run at a time of the clock, and finds the next one: a repeating timer that fell behind runs once,
    /// not once per period missed.
    /// </summary>
    internal void Ran(double time)
    {
        Interlocked.Increment(ref _runs);

        if (Period <= 0)
        {
            _active = false;
            return;
        }

        DueTime += Period;
        if (DueTime <= time) DueTime = time + Period;
    }
}
