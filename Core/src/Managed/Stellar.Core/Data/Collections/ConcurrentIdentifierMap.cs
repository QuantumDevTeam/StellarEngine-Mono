using System.Collections.Concurrent;
using Stellar.Kernel;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// Thread-safe map keyed by identifier value.
/// </summary>
/// <typeparam name="T">The type of stored values.</typeparam>
public sealed class ConcurrentIdentifierMap<T>
    : ConcurrentDictionary<IIdentifier, T>
{
    /// <summary>
    /// Creates an empty map.
    /// </summary>
    public ConcurrentIdentifierMap()
        : base(IdentifierComparer.Instance)
    {
    }

    /// <summary>
    /// Creates a map with initial data.
    /// </summary>
    /// <param name="data">The initial data.</param>
    public ConcurrentIdentifierMap(IEnumerable<KeyValuePair<IIdentifier, T>> data)
        : base(data, IdentifierComparer.Instance)
    {
    }
}
