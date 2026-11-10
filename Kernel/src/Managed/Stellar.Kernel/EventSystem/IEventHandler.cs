using System;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.EventSystem;

/// <summary>
/// A quant that reacts to events.
/// </summary>
/// <remarks>
/// One handler may serve several quants: it works in the context of each quant's primary meta.
/// </remarks>
public interface IEventHandler
    : IQuant
{
    /// <summary>
    /// Handles one event. Generics keep the call free of boxing and give the handler the exact event type
    /// </summary>
    /// <param name="context">Event handling context</param>
    /// <typeparam name="TContext">type of context</typeparam>
    /// <typeparam name="TEvent">handling event type</typeparam>
    void Handle<TContext, TEvent>(ref readonly TContext context)
        where TEvent : struct, IEvent
        where TContext : IEventContext<TEvent>, allows ref struct;

    /// <summary>
    /// handle a batch events from queue
    /// </summary>
    /// <param name="context">execution context</param>
    /// <param name="events">events batch</param>
    /// <typeparam name="TContext">type of context</typeparam>
    /// <typeparam name="TEvent">handling event type</typeparam>
    [Obsolete("Engine DIM. Use Handle<TContext, TEvent> instead")]
    void HandleBatch<TContext, TEvent>(ref TContext context, ref ReadOnlySpan<TEvent> events)
        where TEvent : struct, IEvent
        where TContext : IEventContext<TEvent>, allows ref struct
    {
        foreach (var @event in events)
        {
            context.Event = @event;
            Handle<TContext, TEvent>(in context);
        }
    }
}
