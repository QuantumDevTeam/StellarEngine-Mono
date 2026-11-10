using System.Collections.Generic;

namespace Stellar.Kernel.Quantization;

/// <summary>
/// The fundamental building block of the Stellar Engine - a "Quant" (quantized object).
/// </summary>
/// <remarks>
/// <para>Everything built on the engine is a quant: a game object, an event handler, a High-Level Module, the
/// game itself. Each quant has a unique identity (<see cref="IIdentifiableQuantumObject"/>), one primary
/// meta (<see cref="Meta"/>) and any number of attached metas.</para>
/// <para>A meta quant is not a component. A component belongs to exactly one object, while one meta quant may be
/// attached to several quants and works with each of them through that quant's primary meta. The player and
/// the player's gun can share one event handler meta, which treats them as a character and as an item.</para>
/// </remarks>
public interface IQuant
    : IIdentifiableQuantumObject
{
    /// <summary>
    /// Gets the primary metadata object of this quant. It defines the quant's identity.
    /// </summary>
    /// <value>An <see cref="IMetaQuant"/> instance that describes this quant.</value>
    IMetaQuant Meta { get; }

    /// <summary>
    /// Gets every meta quant of this quant: the primary one first, then the attached ones.
    /// </summary>
    IReadOnlyList<IMetaQuant> Metas { get; }

    /// <summary>
    /// Finds the first meta quant of the requested type.
    /// </summary>
    /// <typeparam name="TMeta">The contract or class of the meta quant.</typeparam>
    /// <returns>The meta quant, or <c>null</c> if this quant has none of that type.</returns>
    TMeta? FindMeta<TMeta>()
        where TMeta : class, IMetaQuant;
}
