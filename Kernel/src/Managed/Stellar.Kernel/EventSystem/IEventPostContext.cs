using Stellar.Kernel.Data.Context;

namespace Stellar.Kernel.EventSystem;

/// <summary>
/// Context an event is posted in.
/// </summary>
/// <typeparam name="TEvent">Type of the event.</typeparam>
public interface IEventPostContext<TEvent>
    : IContext
    where TEvent : struct, IEvent
{
    /// <summary>
    /// Gets or sets the queue to post the event to, or <c>null</c> for the default one.
    /// </summary>
    ILabel? QueueToPost { get; set; }

    /// <summary>
    /// Gets or sets the event being posted.
    /// </summary>
    TEvent Event { get; set; }
}
