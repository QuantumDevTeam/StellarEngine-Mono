using Stellar.Kernel.Timing;

namespace Stellar.Runtime.Timing;

/// <summary>
/// A clock of a <see cref="TimeSystem"/>.
/// </summary>
/// <remarks>
/// Its time is a straight line of real time between two changes: <c>time = anchor time + (now - anchor) * rate</c>.
/// A change of scale or pause first fixes the time reached so far, for the clock and every clock below it, then
/// changes the rate. So the time can be read at any moment, from any thread, and a change never makes it jump.
/// </remarks>
public sealed class Clock
    : IClock
{
    private readonly TimeSystem _system;
    private readonly List<Clock> _children = [];
    private double _anchorReal;
    private double _anchorTime;
    private double _scale;
    private double _fixedStep;
    private bool _paused;

    internal Clock(TimeSystem system, string name, Clock? parent, double scale, double now)
    {
        if (scale < 0 || double.IsNaN(scale)) throw new ArgumentOutOfRangeException(nameof(scale));

        _system = system;
        Name = name;
        ParentClock = parent;
        _scale = scale;
        _anchorReal = now;
        parent?._children.Add(this);
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IClock? Parent => ParentClock;

    /// <inheritdoc/>
    public double Time => TimeAt(_system.Source.Now);

    /// <inheritdoc/>
    public double Scale
    {
        get => _scale;
        set
        {
            if (value < 0 || double.IsNaN(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (ParentClock is null) throw new InvalidOperationException("The system clock runs at real time");

            lock (_system.Gate)
            {
                Anchor(_system.Source.Now);
                _scale = value;
            }
        }
    }

    /// <inheritdoc/>
    public double Rate
    {
        get
        {
            lock (_system.Gate)
            {
                return RateUnlocked;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsPaused
    {
        get
        {
            lock (_system.Gate)
            {
                return _paused;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsRunning
    {
        get
        {
            lock (_system.Gate)
            {
                for (var clock = this; clock is not null; clock = clock.ParentClock)
                    if (clock._paused)
                        return false;

                return true;
            }
        }
    }

    /// <inheritdoc/>
    public double FixedStep
    {
        get => _fixedStep;
        set
        {
            if (value < 0 || double.IsNaN(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _fixedStep = value;
        }
    }

    /// <inheritdoc/>
    public double EffectiveFixedStep
    {
        get
        {
            for (var clock = this; clock is not null; clock = clock.ParentClock)
                if (clock._fixedStep > 0)
                    return clock._fixedStep;

            return 0;
        }
    }

    internal Clock? ParentClock { get; }

    internal IReadOnlyList<Clock> Children => _children;

    private double RateUnlocked => _paused ? 0 : _scale * (ParentClock?.RateUnlocked ?? 1);

    /// <summary>
    /// Gets the time of the clock at a moment of its time source: every clock read at the same moment agrees.
    /// </summary>
    /// <param name="now">The moment, as <see cref="ITimeSource.Now"/> says it.</param>
    /// <returns>The time of the clock.</returns>
    public double TimeAt(double now)
    {
        lock (_system.Gate)
        {
            return _anchorTime + (now - _anchorReal) * RateUnlocked;
        }
    }

    /// <inheritdoc/>
    public void Pause()
    {
        SetPaused(true);
    }

    /// <inheritdoc/>
    public void Resume()
    {
        SetPaused(false);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Name} ({Time:0.###} s, x{Scale})";
    }

    internal void Detach()
    {
        ParentClock?._children.Remove(this);
    }

    internal bool Belongs(TimeSystem system)
    {
        return ReferenceEquals(_system, system);
    }

    private void SetPaused(bool paused)
    {
        if (ParentClock is null) throw new InvalidOperationException("The system clock cannot be paused");

        lock (_system.Gate)
        {
            if (_paused == paused) return;

            Anchor(_system.Source.Now);
            _paused = paused;
        }
    }

    // Fixes the time reached so far, here and below: a change of rate starts from there
    private void Anchor(double now)
    {
        _anchorTime += (now - _anchorReal) * RateUnlocked;
        _anchorReal = now;

        foreach (var child in _children) child.Anchor(now);
    }
}
