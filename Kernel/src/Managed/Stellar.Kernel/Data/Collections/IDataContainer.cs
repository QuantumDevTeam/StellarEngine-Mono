using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Data.Collections;

/// <summary>
/// A container of the engine: data of the world held by a quant, always in the registry of containers of its engine.
/// </summary>
/// <remarks>
/// <para>A container has an identity, so it is found anywhere by its identifier: the registry of containers gives
/// the container, the container gives the item. A roster of actors and a player in it:
/// <c>containers.Get&lt;Roster&lt;Actor&gt;&gt;(rosterId).Get&lt;Player&gt;(playerId)</c>.</para>
/// <para>A sealed container never changes again.</para>
/// </remarks>
public interface IDataContainer
    : IRegistrableQuant
{
    /// <summary>
    /// Gets the number of items.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets whether the container is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets whether the container is sealed: it never changes again.
    /// </summary>
    bool IsSealed { get; }
}

/// <summary>
/// A container of items by key: a table by identifier, a data table by row name.
/// </summary>
/// <typeparam name="TKey">The key of an item.</typeparam>
/// <typeparam name="TValue">The item.</typeparam>
public interface IDataContainer<TKey, TValue>
    : IDataContainer, IEnumerable<TValue>
    where TKey : notnull
{
    /// <summary>
    /// Gets a snapshot of the keys.
    /// </summary>
    IReadOnlyCollection<TKey> Keys { get; }

    /// <summary>
    /// Gets a snapshot of the items.
    /// </summary>
    IReadOnlyCollection<TValue> Values { get; }

    /// <summary>
    /// Gets whether an item has the key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> if one has.</returns>
    bool ContainsKey(TKey key);

    /// <summary>
    /// Finds an item.
    /// </summary>
    /// <param name="key">Its key.</param>
    /// <param name="value">The item, or <c>default</c>.</param>
    /// <returns><c>true</c> if the container has it.</returns>
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Gets an item.
    /// </summary>
    /// <param name="key">Its key.</param>
    /// <returns>The item.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the container has none with the key.</exception>
    TValue Get(TKey key);
}
