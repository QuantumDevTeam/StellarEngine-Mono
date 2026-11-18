using System.Collections;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// An ordered list, safe from any thread, for what is read far more often than it changes: the systems of a loop,
/// the handlers of an event, the layers of a scene.
/// </summary>
/// <remarks>
/// <para>Reads take no lock: the items live in an array that is never changed, a change makes a new one. Reading,
/// indexing and enumerating see one consistent state, even while another thread changes the list; a writer pays a
/// copy. For lists that change on every frame, a <see cref="List{T}"/> owned by one thread is the better tool.</para>
/// <para><see cref="Changed"/> is raised after each change, out of the lock, on the thread that changed the list.</para>
/// </remarks>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed class ConcurrentList<T>
    : IReadOnlyList<T>
{
    private readonly Lock _writing = new();
    private readonly IEqualityComparer<T> _comparer;
    private volatile T[] _items;

    /// <summary>
    /// Makes a list.
    /// </summary>
    /// <param name="items">The first items, in order; none if <c>null</c>.</param>
    /// <param name="comparer">How items compare, for <see cref="Contains"/>, <see cref="IndexOf"/>, <see cref="Remove"/>.</param>
    public ConcurrentList(IEnumerable<T>? items = null, IEqualityComparer<T>? comparer = null)
    {
        _items = items is null ? [] : items.ToArray();
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Raised after each change, out of the lock.
    /// </summary>
    public event Action<CollectionChange<T>>? Changed;

    /// <inheritdoc/>
    public int Count => _items.Length;

    /// <summary>
    /// Gets or replaces the item at an index.
    /// </summary>
    /// <param name="index">The index, from 0.</param>
    public T this[int index]
    {
        get => _items[index];
        set
        {
            T old;
            lock (_writing)
            {
                var items = (T[])_items.Clone();
                old = items[index];
                items[index] = value;
                _items = items;
            }

            Changed?.Invoke(new CollectionChange<T>(ChangeKind.Replaced, value, index, old));
        }
    }

    /// <summary>
    /// Gets the items as they are now, without a copy. The span never changes: a change of the list makes a new one.
    /// </summary>
    /// <returns>The items.</returns>
    public ReadOnlySpan<T> AsSpan()
    {
        return _items;
    }

    /// <summary>
    /// Copies the items as they are now.
    /// </summary>
    /// <returns>A new array.</returns>
    public T[] ToArray()
    {
        return (T[])_items.Clone();
    }

    /// <summary>
    /// Copies a part of the list. A negative index counts from the end, as in Python: <c>Slice(-2)</c> is the last two.
    /// </summary>
    /// <param name="start">The first index.</param>
    /// <param name="end">The index after the last one; the end of the list if <c>null</c>.</param>
    /// <returns>A new array; empty if the range is.</returns>
    public T[] Slice(int start, int? end = null)
    {
        var items = _items;
        var from = Math.Clamp(start < 0 ? items.Length + start : start, 0, items.Length);
        var to = Math.Clamp(end is null ? items.Length : end.Value < 0 ? items.Length + end.Value : end.Value, 0, items.Length);
        return to <= from ? [] : items[from..to];
    }

    /// <summary>
    /// Adds an item at the end.
    /// </summary>
    /// <param name="item">The item.</param>
    public void Add(T item)
    {
        int index;
        lock (_writing)
        {
            index = _items.Length;
            _items = [.. _items, item];
        }

        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Added, item, index));
    }

    /// <summary>
    /// Adds items at the end, with one copy.
    /// </summary>
    /// <param name="items">The items, in order.</param>
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var added = items.ToArray();
        if (added.Length == 0) return;

        int start;
        lock (_writing)
        {
            start = _items.Length;
            _items = [.. _items, .. added];
        }

        if (Changed is { } changed)
            for (var i = 0; i < added.Length; i++) changed(new CollectionChange<T>(ChangeKind.Added, added[i], start + i));
    }

    /// <summary>
    /// Inserts an item at an index.
    /// </summary>
    /// <param name="index">Where; <see cref="Count"/> adds at the end.</param>
    /// <param name="item">The item.</param>
    public void Insert(int index, T item)
    {
        lock (_writing)
        {
            var items = _items;
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)items.Length, nameof(index));
            _items = [.. items.AsSpan(0, index), item, .. items.AsSpan(index)];
        }

        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Added, item, index));
    }

    /// <summary>
    /// Removes the first occurrence of an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if it was there.</returns>
    public bool Remove(T item)
    {
        int index;
        T removed;
        lock (_writing)
        {
            index = IndexOf(item);
            if (index < 0) return false;
            removed = RemoveAtLocked(index);
        }

        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Removed, removed, index));
        return true;
    }

    /// <summary>
    /// Removes the item at an index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The removed item.</returns>
    public T RemoveAt(int index)
    {
        T removed;
        lock (_writing)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_items.Length, nameof(index));
            removed = RemoveAtLocked(index);
        }

        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Removed, removed, index));
        return removed;
    }

    /// <summary>
    /// Removes and gives back the last item, as in Python.
    /// </summary>
    /// <param name="item">The item, or <c>default</c>.</param>
    /// <returns><c>true</c> if the list had one.</returns>
    public bool TryPop(out T item)
    {
        int index;
        lock (_writing)
        {
            index = _items.Length - 1;
            if (index < 0)
            {
                item = default!;
                return false;
            }

            item = RemoveAtLocked(index);
        }

        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Removed, item, index));
        return true;
    }

    /// <summary>
    /// Removes every item.
    /// </summary>
    public void Clear()
    {
        lock (_writing)
        {
            if (_items.Length == 0) return;
            _items = [];
        }

        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Cleared, default!));
    }

    /// <summary>
    /// Sorts the list, as one change.
    /// </summary>
    /// <param name="comparer">The order; the default one of <typeparamref name="T"/> if <c>null</c>.</param>
    public void Sort(IComparer<T>? comparer = null)
    {
        lock (_writing)
        {
            var items = (T[])_items.Clone();
            Array.Sort(items, comparer);
            _items = items;
        }

        Changed?.Invoke(new CollectionChange<T>(ChangeKind.Replaced, default!));
    }

    /// <summary>
    /// Gets the index of the first occurrence of an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The index, or -1.</returns>
    public int IndexOf(T item)
    {
        var items = _items;
        for (var i = 0; i < items.Length; i++)
            if (_comparer.Equals(items[i], item))
                return i;

        return -1;
    }

    /// <summary>
    /// Gets whether an item is in the list.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if it is.</returns>
    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    /// <summary>
    /// Enumerates the items as they were when the enumeration started.
    /// </summary>
    public SnapshotEnumerator<T> GetEnumerator()
    {
        return new SnapshotEnumerator<T>(_items);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private T RemoveAtLocked(int index)
    {
        var items = _items;
        var removed = items[index];
        _items = [.. items.AsSpan(0, index), .. items.AsSpan(index + 1)];
        return removed;
    }
}
