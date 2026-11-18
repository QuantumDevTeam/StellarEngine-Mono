using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Data.Catalog;

/// <summary>
/// Values found by a key: what can be read of a catalog.
/// </summary>
/// <remarks>
/// <para>A catalog is the other half of the storage of the engine. A registry holds objects with an identity by their
/// identifiers (<see cref="Registry.IRegistry{T}"/>); a catalog holds anything by a key of its own: services by their
/// contract, content types by extension, drivers by scheme, domains by name.</para>
/// <para>How keys compare is up to the catalog (names in any case, for instance), and says so.</para>
/// </remarks>
/// <typeparam name="TKey">The key.</typeparam>
/// <typeparam name="TValue">The value.</typeparam>
public interface IReadOnlyCatalog<TKey, TValue>
    : IQuantumObject, IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    /// <summary>
    /// Gets a snapshot of the keys.
    /// </summary>
    IReadOnlyCollection<TKey> Keys { get; }

    /// <summary>
    /// Gets a snapshot of the values.
    /// </summary>
    IReadOnlyCollection<TValue> Values { get; }

    /// <summary>
    /// Gets whether the catalog has a value for a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> if it has one.</returns>
    bool Contains(TKey key);

    /// <summary>
    /// Finds the value of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, or <c>default</c>.</param>
    /// <returns><c>true</c> if the catalog has a value for the key.</returns>
    bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// Gets the value of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the catalog has no value for the key, or an exception of the catalog that says more.
    /// </exception>
    TValue Get(TKey key);
}
