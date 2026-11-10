namespace Stellar.Kernel.EventSystem;

/// <summary>
/// Describes a kind of event.
/// </summary>
public interface IEventType
    : ILabel
{
    /// <summary>
    /// Signed event type value: below zero for engine and system events, above zero for custom ones.
    /// The identifier from <see cref="ILabel"/> identifies the type in registries, this field is what the
    /// event type is matched by.
    /// </summary>
    short TypeValue { get; }
}
