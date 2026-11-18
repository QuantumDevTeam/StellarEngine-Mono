using System.Collections;
using System.Collections.Concurrent;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// A set, safe from any thread: lookups take no lock, enumerations see a snapshot.
/// </summary>
/// <remarks>
/// <para>The snapshot is built once after a change and then shared by every reader, so enumerating a set that
/// does not change costs nothing. <see cref="Changed"/> is raised after each change, on the thread that made it.</para>
/// <para>The operations of sets (<see cref="Union"/>, <see cref="Intersect"/>, <see cref="Except"/>) make new sets.</para>
/// </remarks>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed class ConcurrentSet<T>
    : IReadOnlyCollection<T>
    where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _items;
    private readonly SnapshotCache<T> _snapshot;

    /// <summary>
    /// Makes a set.
    /// </summary>
    /// <param name="items">The first items; none if <c>null</c>.</param>
    /// <param name="comparer">How items compare; the default comparer of <typeparamref name="T"/> if <c>null</c>.</param>
    public ConcurrentSet(IEnumerable<T>? items = null, IEqualityComparer<T>? comparer = null)
    {
        Comparer = comparer ?? EqualityComparer<T>.Default;
        _items = new ConcurrentDictionary<T, byte>(Comparer);
        _snapshot = new SnapshotCache<T>(() => _items.Keys.ToArray());
        if (items is not null)
            foreach (var item in items) _items.TryAdd(item, 0);
    }

    /// <summary>
    /// Raised after each change, out of any lock.
    /// </summary>
    public event Action<CollectionChange<T>>? Changed;

    /// <summary>
    /// Gets how items compare.
    /// </summary>
    public IEqualityComparer<T> Comparer { get; }

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <summary>
    /// Gets the items as they are now: one array, shared until the set changes. Do not change it.
    /// </summary>
    public IReadOnlyList<T> Snapshot => _snapshot.Get();

    /// <summary>
    /// Adds an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if it was not there.</returns>
    public bool Add(T item)
    {
        if (!_items.TryAdd(item, 0)) return false;

        _snapshot.Invalidate();
        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Added, item));
        return true;
    }

    /// <summary>
    /// Adds items.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <returns>How many were not there.</returns>
    public int AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var added = 0;
        foreach (var item in items)
            if (Add(item))
                added++;
        return added;
    }

    /// <summary>
    /// Removes an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if it was there.</returns>
    public bool Remove(T item)
    {
        if (!_items.TryRemove(item, out _)) return false;

        _snapshot.Invalidate();
        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Removed, item));
        return true;
    }

    /// <summary>
    /// Removes every item.
    /// </summary>
    public void Clear()
    {
        if (_items.IsEmpty) return;

        _items.Clear();
        _snapshot.Invalidate();
        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Cleared, default!));
    }

    /// <summary>
    /// Gets whether an item is in the set.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if it is.</returns>
    public bool Contains(T item)
    {
        return _items.ContainsKey(item);
    }

    /// <summary>
    /// Makes the set of the items of this set and of others.
    /// </summary>
    /// <param name="other">The other items.</param>
    /// <returns>A new set, with the comparer of this one.</returns>
    public ConcurrentSet<T> Union(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new ConcurrentSet<T>(Snapshot.Concat(other), Comparer);
    }

    /// <summary>
    /// Makes the set of the items of this set that are also in others.
    /// </summary>
    /// <param name="other">The other items.</param>
    /// <returns>A new set, with the comparer of this one.</returns>
    public ConcurrentSet<T> Intersect(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var others = other.ToHashSet(Comparer);
        return new ConcurrentSet<T>(Snapshot.Where(others.Contains), Comparer);
    }

    /// <summary>
    /// Makes the set of the items of this set that are not in others.
    /// </summary>
    /// <param name="other">The other items.</param>
    /// <returns>A new set, with the comparer of this one.</returns>
    public ConcurrentSet<T> Except(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var others = other.ToHashSet(Comparer);
        return new ConcurrentSet<T>(Snapshot.Where(item => !others.Contains(item)), Comparer);
    }

    /// <summary>
    /// Gets whether this set and others share an item.
    /// </summary>
    /// <param name="other">The other items.</param>
    /// <returns><c>true</c> if they do.</returns>
    public bool Overlaps(IEnumerable<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Any(Contains);
    }

    /// <summary>
    /// Enumerates the items as they were when the enumeration started.
    /// </summary>
    public SnapshotEnumerator<T> GetEnumerator()
    {
        return new SnapshotEnumerator<T>(_snapshot.Get());
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
