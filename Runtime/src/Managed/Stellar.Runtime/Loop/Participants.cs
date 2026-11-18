using System.Collections.Concurrent;
using System.Reflection;
using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Profiling;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Timing;

namespace Stellar.Runtime.Loop;

/// <summary>
/// A participant of a main loop, with what it takes part in.
/// </summary>
internal sealed class Participant(IQuant quant, IClock clock, int order, long sequence)
{
    // The phases each type of participant has code for, worked out once per type
    private static readonly ConcurrentDictionary<Type, bool[]> PhasesOfTypes = new();

    public IQuant Quant { get; } = quant;

    public IClock Clock { get; } = clock;

    public int Order { get; } = order;

    public long Sequence { get; } = sequence;

    public IUpdatable? Updatable { get; } = quant as IUpdatable;

    public IFixedUpdatable? FixedUpdatable { get; } = quant as IFixedUpdatable;

    public IRenderable? Renderable { get; } = quant as IRenderable;

    /// <summary>
    /// Gets the profiler marker of its calls: the full name of its type, registered when a profiler first asks.
    /// </summary>
    public int Marker => _marker != 0 ? _marker : _marker = ProfilerMarkers.Register(Quant.GetType().FullName ?? Quant.GetType().Name, ProfilerCategory.Application);

    private int _marker;

    /// <summary>
    /// Gets or sets the state of its clock in the current frame: set by the loop when it samples the clocks.
    /// </summary>
    public ClockFrame? Frame { get; set; }

    public static bool TakesPart(IQuant quant)
    {
        return quant is IUpdatable or IFixedUpdatable or IRenderable;
    }

    /// <summary>
    /// Gets whether it has code for a phase: a phase it only has the empty default of is not called.
    /// </summary>
    public bool TakesPartIn(FramePhase phase)
    {
        return PhasesOfTypes.GetOrAdd(Quant.GetType(), static type => PhasesOf(type))[(int)phase];
    }

    private static bool[] PhasesOf(Type type)
    {
        var phases = new bool[Enum.GetValues<FramePhase>().Length];
        if (typeof(IUpdatable).IsAssignableFrom(type))
        {
            phases[(int)FramePhase.PreUpdate] = HasCode(type, typeof(IUpdatable), nameof(IUpdatable.PreUpdate));
            phases[(int)FramePhase.Update] = HasCode(type, typeof(IUpdatable), nameof(IUpdatable.Update));
            phases[(int)FramePhase.PostUpdate] = HasCode(type, typeof(IUpdatable), nameof(IUpdatable.PostUpdate));
        }

        if (typeof(IFixedUpdatable).IsAssignableFrom(type))
            phases[(int)FramePhase.FixedUpdate] = HasCode(type, typeof(IFixedUpdatable), nameof(IFixedUpdatable.FixedUpdate));

        if (typeof(IRenderable).IsAssignableFrom(type))
        {
            phases[(int)FramePhase.PreRender] = HasCode(type, typeof(IRenderable), nameof(IRenderable.PreRender));
            phases[(int)FramePhase.Render] = HasCode(type, typeof(IRenderable), nameof(IRenderable.Render));
            phases[(int)FramePhase.PostRender] = HasCode(type, typeof(IRenderable), nameof(IRenderable.PostRender));
        }

        return phases;
    }

    // A method implemented anywhere but in the contract itself (by the class, or by an interface that overrides it)
    private static bool HasCode(Type type, Type contract, string method)
    {
        try
        {
            var map = type.GetInterfaceMap(contract);
            for (var i = 0; i < map.InterfaceMethods.Length; i++)
                if (map.InterfaceMethods[i].Name == method)
                    return map.TargetMethods[i] is not { } target || target.DeclaringType != contract;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            // A type the map cannot be made for: called, as if it had code
        }

        return true;
    }
}

/// <summary>
/// The state of a clock in the current frame of a loop.
/// </summary>
internal sealed class ClockFrame
{
    public double Time;

    public double Delta;

    public bool Running;

    public double FixedStep;

    public double Accumulator;

    public int FixedSteps;

    public bool Used;

    /// <summary>
    /// Set when the loop forgets the clock: a participant that still holds the frame looks the clock up again.
    /// </summary>
    public bool Forgotten;
}

/// <summary>
/// The participants of a loop. Changes come from any thread and apply at the start of a frame, so a frame always
/// walks a list nobody changes under it. The list is kept by order, then by arrival, and so is the list of each phase.
/// </summary>
internal sealed class Participants
{
    private static readonly int PhaseCount = Enum.GetValues<FramePhase>().Length;

    private readonly object _gate = new();
    private readonly List<Participant> _items = [];
    private readonly List<(IQuant Quant, Participant? Added)> _pending = [];
    private Participant[][] _phases = Enumerable.Range(0, PhaseCount).Select(_ => Array.Empty<Participant>()).ToArray();
    private volatile bool _hasPending;
    private long _sequence;

    /// <summary>
    /// Gets the participants of the current frame. Only the thread of the loop reads it, between two
    /// <see cref="Apply"/>.
    /// </summary>
    public List<Participant> Items => _items;

    /// <summary>
    /// Gets the participants that have code for a phase, in order. Read as <see cref="Items"/>.
    /// </summary>
    public Participant[] In(FramePhase phase)
    {
        return _phases[(int)phase];
    }

    public void Add(IQuant quant, IClock clock, int order)
    {
        lock (_gate)
        {
            _pending.Add((quant, new Participant(quant, clock, order, _sequence++)));
            _hasPending = true;
        }
    }

    public bool Remove(IQuant quant)
    {
        lock (_gate)
        {
            var present = _items.Exists(item => ReferenceEquals(item.Quant, quant));

            foreach (var (pending, added) in _pending)
                if (ReferenceEquals(pending, quant))
                    present = added is not null;

            if (present)
            {
                _pending.Add((quant, null));
                _hasPending = true;
            }

            return present;
        }
    }

    public void Apply()
    {
        if (!_hasPending) return;

        lock (_gate)
        {
            if (_pending.Count == 0) return;

            // The last change of each participant is the one that counts
            var changes = new Dictionary<IQuant, Participant?>(ReferenceEqualityComparer.Instance);
            foreach (var (quant, added) in _pending) changes[quant] = added;

            _items.RemoveAll(item => changes.ContainsKey(item.Quant));
            foreach (var added in changes.Values)
                if (added is not null)
                    _items.Add(added);

            _pending.Clear();
            _hasPending = false;
            _items.Sort(static (left, right) => left.Order != right.Order
                ? left.Order.CompareTo(right.Order)
                : left.Sequence.CompareTo(right.Sequence));

            var phases = new Participant[PhaseCount][];
            for (var phase = 0; phase < PhaseCount; phase++)
                phases[phase] = _items.Where(item => item.TakesPartIn((FramePhase)phase)).ToArray();
            _phases = phases;
        }
    }
}
