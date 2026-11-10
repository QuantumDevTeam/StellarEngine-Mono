using System;
using Stellar.Kernel.Data.Context;

namespace Stellar.Kernel.EventSystem;

/// <summary>
/// An event: plain structure data that travels through the event bus.
/// </summary>
/// <remarks>Events are context data, so a handler always receives one inside a context.</remarks>
public interface IEvent
    : IContextData
{
    /// <summary>
    /// Event type
    /// </summary>
    IEventType EventType { get; }

    /// <summary>
    /// tyme of event creation
    /// </summary>
    DateTime TimeStamp { get; }

    /// <summary>
    /// mark event as reusable
    /// </summary>
    bool CanBeReused { get; }

    /// <summary>
    /// Property getter that called during processing and tills the bus the event needs to be passed to handler 
    /// </summary>
    bool ShouldProcessNow { get; }
}
