using System.Collections.Generic;
using Stellar.Kernel.Data.Collections;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.EventSystem;

/// <summary>
/// Type-erased view of an event queue, used by the bus to process queues of any event type.
/// </summary>
public interface IEventQueueBase
    : IRegistrableQuant
{
    /// <summary>
    /// Gets the number of queued events.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a value indicating whether the queue has no events.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Prepares the queue for dispatching.
    /// </summary>
    void Prepare();

    /// <summary>
    /// Delivers the queued events to handlers.
    /// </summary>
    void DispatchTo(IEnumerable<IEventHandler> handlers);

    /// <summary>
    /// Removes all queued events.
    /// </summary>
    void Clear();
}

/// <summary>
/// A labeled queue of events of one type.
/// </summary>
/// <typeparam name="TEvent">Type of the event.</typeparam>
public interface IEventQueue<TEvent>
    : IQueueBase<TEvent>, IEventQueueBase, ILabeled
    where TEvent : struct, IEvent
{
    /// <summary>
    /// Adds an event.
    /// </summary>
    new void Enqueue(TEvent item);

    /// <summary>
    /// Takes the next event.
    /// </summary>
    new bool TryDequeue(out TEvent item);
}
