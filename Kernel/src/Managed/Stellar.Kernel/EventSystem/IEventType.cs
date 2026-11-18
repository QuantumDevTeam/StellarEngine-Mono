using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.EventSystem;

/// <summary>
/// Describes a kind of event.
/// </summary>
public interface IEventType
    : IMetaQuant, ILabeled
{
    /// <summary>
    /// Signed event type value: below zero for engine and system events, above zero for custom ones.
    /// The identifier of the type identifies it in registries, this field is what the
    /// event type is matched by.
    /// </summary>
    short TypeValue { get; }
}
