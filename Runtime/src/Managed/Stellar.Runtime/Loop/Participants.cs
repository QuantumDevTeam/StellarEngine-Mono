using Stellar.Kernel.Lifecycle;
using Stellar.Kernel.Quantization;
using Stellar.Kernel.Timing;

namespace Stellar.Runtime.Loop;

/// <summary>
/// A participant of a main loop, with what it takes part in.
/// </summary>
internal sealed class Participant(IQuant quant, IClock clock, int order, long sequence)
{
    public IQuant Quant { get; } = quant;

    public IClock Clock { get; } = clock;

    public int Order { get; } = order;

    public long Sequence { get; } = sequence;

    public IUpdatable? Updatable { get; } = quant as IUpdatable;

    public IFixedUpdatable? FixedUpdatable { get; } = quant as IFixedUpdatable;

    public IRenderable? Renderable { get; } = quant as IRenderable;

    public static bool TakesPart(IQuant quant)
    {
        return quant is IUpdatable or IFixedUpdatable or IRenderable;
    }
}

/// <summary>
/// The participants of a loop. Changes come from any thread and apply at the start of a frame, so a frame always
/// walks a list nobody changes under it. The list is kept by order, then by arrival.
/// </summary>
internal sealed class Participants
{
    private readonly object _gate = new();
    private readonly List<Participant> _items = [];
    private readonly List<(IQuant Quant, Participant? Added)> _pending = [];
    private long _sequence;

    /// <summary>
    /// Gets the participants of the current frame. Only the thread of the loop reads it, between two
    /// <see cref="Apply"/>.
    /// </summary>
    public List<Participant> Items => _items;

    public void Add(IQuant quant, IClock clock, int order)
    {
        lock (_gate)
        {
            _pending.Add((quant, new Participant(quant, clock, order, _sequence++)));
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

            if (present) _pending.Add((quant, null));
            return present;
        }
    }

    public void Apply()
    {
        lock (_gate)
        {
            if (_pending.Count == 0) return;

            foreach (var (quant, added) in _pending)
            {
                _items.RemoveAll(item => ReferenceEquals(item.Quant, quant));
                if (added is not null) _items.Add(added);
            }

            _pending.Clear();
            _items.Sort((left, right) => left.Order != right.Order
                ? left.Order.CompareTo(right.Order)
                : left.Sequence.CompareTo(right.Sequence));
        }
    }
}
