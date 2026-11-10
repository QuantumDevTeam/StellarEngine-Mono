using System;
using System.Threading;
using System.Threading.Tasks;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.EventSystem;

/// <summary>
/// Routes events from senders to subscribed handlers, directly or through named queues.
/// </summary>
public interface IEventBus
    : IRegistrableQuant
{
    /// <summary>
    /// Adds a queue to the bus.
    /// </summary>
    void AddQueue(IEventQueueBase queue);

    /// <summary>
    /// Makes a handler known to the bus.
    /// </summary>
    void Register(IEventHandler handler);

    /// <summary>
    /// Makes an event type known to the bus.
    /// </summary>
    void Register(IEventType eventType);

    /// <summary>
    /// Subscribes a handler to a queue, for one event type or for all of them.
    /// </summary>
    void Subscribe(IEventHandler handler, ILabel queue, IEventType? eventType = null, float handlerPriority = 0);

    /// <summary>
    /// Removes a subscription.
    /// </summary>
    void Unsubscribe(IEventHandler handler, ILabel queue, IEventType? eventType = null);

    /// <summary>
    /// Delivers events to the handlers right away.
    /// </summary>
    void Emit<TContext, TEvent>(ref TContext context, ReadOnlySpan<TEvent> events)
        where TEvent : struct, IEvent
        where TContext : IEventPostContext<TEvent>, allows ref struct;

    /// <summary>
    /// Delivers events to the handlers asynchronously.
    /// </summary>
    Task EmitAsync<TContext, TEvent>(ref TContext context, ReadOnlySpan<TEvent> events,
        CancellationToken cancellationToken = default)
        where TEvent : struct, IEvent
        where TContext : IEventPostContext<TEvent>, allows ref struct;

    /// <summary>
    /// Puts events into a queue, to be delivered by <see cref="Process"/>.
    /// </summary>
    void Enqueue<TContext, TEvent>(ref TContext context, ReadOnlySpan<TEvent> events)
        where TEvent : struct, IEvent
        where TContext : IEventPostContext<TEvent>, allows ref struct;

    /// <summary>
    /// Delivers the queued events of one queue, or of all queues when it is <c>null</c>.
    /// </summary>
    void Process(ILabel? queue);

    /// <summary>
    /// Delivers the queued events asynchronously.
    /// </summary>
    Task ProcessAsync(ILabel? queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all queued events.
    /// </summary>
    void Clear();
}
