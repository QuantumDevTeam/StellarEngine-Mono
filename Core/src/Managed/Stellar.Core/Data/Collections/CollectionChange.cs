namespace Stellar.Core.Data.Collections;

/// <summary>
/// What happened to a collection.
/// </summary>
public enum ChangeKind : byte
{
    /// <summary>An item came in.</summary>
    Added,

    /// <summary>An item went out.</summary>
    Removed,

    /// <summary>An item took the place of another.</summary>
    Replaced,

    /// <summary>Everything went out.</summary>
    Cleared
}

/// <summary>
/// A change of a list or a set.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Item">The item that came in or went out; for <see cref="ChangeKind.Replaced"/>, the new one.</param>
/// <param name="Index">Where, in a list; -1 in a set, or for <see cref="ChangeKind.Cleared"/>.</param>
/// <param name="OldItem">The item that was replaced; <c>default</c> otherwise.</param>
/// <typeparam name="T">The type of the items.</typeparam>
public readonly record struct CollectionChange<T>(ChangeKind Kind, T Item, int Index = -1, T OldItem = default!);

/// <summary>
/// A change of a map.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Key">The key; <c>default</c> for <see cref="ChangeKind.Cleared"/>.</param>
/// <param name="Value">The value that came in or went out; for <see cref="ChangeKind.Replaced"/>, the new one.</param>
/// <param name="OldValue">The value that was replaced; <c>default</c> otherwise.</param>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
public readonly record struct MapChange<TKey, TValue>(ChangeKind Kind, TKey Key, TValue Value, TValue OldValue = default!);

/// <summary>
/// An array that stays valid until the collection changes: built once per change, then shared by every reader.
/// </summary>
/// <param name="build">Builds the array from the collection as it is; made once, so a read allocates nothing.</param>
/// <typeparam name="T">The type of the items.</typeparam>
internal sealed class SnapshotCache<T>(Func<T[]> build)
{
    private int _version;
    private volatile Holder? _holder;

    /// <summary>
    /// Marks the snapshot out of date: called after every change.
    /// </summary>
    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
    }

    /// <summary>
    /// Gets the snapshot, building it if the collection changed since.
    /// </summary>
    public T[] Get()
    {
        var version = Volatile.Read(ref _version);
        var holder = _holder;
        if (holder is not null && holder.Version == version) return holder.Items;

        // Built after reading the version: a change in between only makes the next reader build again
        var items = build();
        _holder = new Holder(version, items);
        return items;
    }

    private sealed class Holder(int version, T[] items)
    {
        public readonly int Version = version;
        public readonly T[] Items = items;
    }
}

/// <summary>
/// Enumerates a snapshot of a collection without allocating: what <c>foreach</c> over the collections of the engine uses.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public struct SnapshotEnumerator<T>
    : IEnumerator<T>
{
    private readonly T[] _items;
    private int _index;

    internal SnapshotEnumerator(T[] items)
    {
        _items = items;
        _index = -1;
    }

    /// <inheritdoc/>
    public readonly T Current => _items[_index];

    readonly object? System.Collections.IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        var next = _index + 1;
        if (next >= _items.Length) return false;

        _index = next;
        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _index = -1;
    }

    /// <inheritdoc/>
    public readonly void Dispose()
    {
    }
}
