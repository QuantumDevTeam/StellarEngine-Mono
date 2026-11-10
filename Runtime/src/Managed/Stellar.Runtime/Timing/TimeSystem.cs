using Stellar.Kernel.Timing;

namespace Stellar.Runtime.Timing;

/// <summary>
/// The clocks of one engine: the system clock, the engine clock below it, and the clocks made by modules and games.
/// </summary>
public sealed class TimeSystem
    : ITimeSystem
{
    private readonly Dictionary<string, Clock> _clocks = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSystem"/> class.
    /// </summary>
    /// <param name="source">Where the time comes from; the machine by default.</param>
    /// <param name="fixedStep">The fixed step of the engine clock, in seconds.</param>
    public TimeSystem(ITimeSource? source = null, double fixedStep = 1.0 / 60)
    {
        Source = source ?? new StopwatchTimeSource();

        var now = Source.Now;
        SystemClock = new Clock(this, ITimeSystem.SystemClockName, null, 1, now);
        EngineClock = new Clock(this, ITimeSystem.EngineClockName, SystemClock, 1, now) { FixedStep = fixedStep };

        _clocks[SystemClock.Name] = SystemClock;
        _clocks[EngineClock.Name] = EngineClock;
    }

    /// <inheritdoc/>
    public ITimeSource Source { get; }

    /// <inheritdoc/>
    public IClock System => SystemClock;

    /// <inheritdoc/>
    public IClock Engine => EngineClock;

    /// <summary>
    /// Gets the system clock.
    /// </summary>
    public Clock SystemClock { get; }

    /// <summary>
    /// Gets the engine clock.
    /// </summary>
    public Clock EngineClock { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<IClock> Clocks
    {
        get
        {
            lock (Gate)
            {
                return _clocks.Values.ToList();
            }
        }
    }

    internal object Gate { get; } = new();

    /// <inheritdoc/>
    public IClock CreateClock(string name, IClock? parent = null, double scale = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var parentClock = parent switch
        {
            null => EngineClock,
            Clock clock when clock.Belongs(this) => clock,
            _ => throw new ArgumentException("The parent clock belongs to another time system", nameof(parent))
        };

        lock (Gate)
        {
            if (_clocks.ContainsKey(name))
                throw new ArgumentException($"There is a clock named {name} already", nameof(name));

            var created = new Clock(this, name, parentClock, scale, Source.Now);
            _clocks[name] = created;
            return created;
        }
    }

    /// <inheritdoc/>
    public IClock? Find(string name)
    {
        lock (Gate)
        {
            return _clocks.GetValueOrDefault(name);
        }
    }

    /// <inheritdoc/>
    public bool Remove(IClock clock)
    {
        if (clock is not Clock removed || ReferenceEquals(removed, SystemClock) ||
            ReferenceEquals(removed, EngineClock))
            return false;

        lock (Gate)
        {
            if (!_clocks.TryGetValue(removed.Name, out var known) || !ReferenceEquals(known, removed)) return false;

            RemoveTree(removed);
            removed.Detach();
            return true;
        }
    }

    private void RemoveTree(Clock clock)
    {
        foreach (var child in clock.Children.ToList()) RemoveTree(child);
        _clocks.Remove(clock.Name);
    }
}
