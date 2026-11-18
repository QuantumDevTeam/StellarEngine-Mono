using Stellar.Kernel.Quantization;

namespace Stellar.Kernel;

/// <summary>
/// Extends <see cref="IIdentifiableQuantumObject"/> with a human‑readable label.
/// </summary>
/// <remarks>
/// The label names the object for people (scripts, tools, logs); the engine finds it by its identifier.
/// </remarks>
public interface ILabeled
    : IIdentifiableQuantumObject
{
    /// <summary>
    /// Gets the label associated with this object.
    /// </summary>
    /// <value>The label: an identifier and a name.</value>
    Label Label { get; }
}
