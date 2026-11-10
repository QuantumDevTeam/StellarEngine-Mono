using Stellar.Kernel.Data.Context;

namespace Stellar.Kernel.EventSystem;

/// <summary>
/// Context a handler receives an event in.
/// </summary>
/// <typeparam name="TEvent">Type of the event.</typeparam>
public interface IEventContext<TEvent>
    : IContext
    where TEvent : struct, IEvent
{
    /// <summary>
    /// The event carried by the context.
    /// </summary>
    TEvent Event { get; set; }
}
