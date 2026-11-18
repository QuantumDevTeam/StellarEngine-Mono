using Stellar.Core.Data.Collections;
using Stellar.Kernel.Data.Catalog;

namespace Stellar.Core.Data.Catalog;

/// <summary>
/// The catalog of the engine: values by a key of their own, safe from any thread.
/// </summary>
/// <remarks>
/// <para>A <see cref="ConcurrentMap{TKey,TValue}"/> as an engine catalog: lookups take no lock, enumerations see a
/// snapshot, <see cref="ConcurrentMap{TKey,TValue}.Changed"/> tells what changed. A derived catalog brings keys to one
/// form, refuses what it cannot hold and says what is missing, through the hooks of the map. A catalog refuses
/// <c>null</c> values.</para>
/// </remarks>
/// <typeparam name="TKey">The key.</typeparam>
/// <typeparam name="TValue">The value.</typeparam>
public class Catalog<TKey, TValue>
    : ConcurrentMap<TKey, TValue>, ICatalog<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Makes an empty catalog.
    /// </summary>
    /// <param name="comparer">How keys compare; the default comparer of <typeparamref name="TKey"/> if <c>null</c>.</param>
    public Catalog(IEqualityComparer<TKey>? comparer = null)
        : base(comparer)
    {
    }

    /// <inheritdoc/>
    public bool Contains(TKey key)
    {
        return ContainsKey(key);
    }

    /// <inheritdoc/>
    protected override void Validate(TKey key, TValue value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value), $"No value for '{key}'");
    }
}
