using System.Collections;
using Stellar.Kernel;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// High-performance thread-safe dictionary for <see cref="IIdentifier"/> keys.
/// Uses snapshot-based iteration to avoid per-frame allocations and supports immediate visibility for "hot" items.
/// </summary>
/// <typeparam name="T">The type of stored values.</typeparam>
/// <remarks>
/// <para>Optimized for read-heavy / iteration-heavy scenarios with rare writes.
/// Iteration over the snapshot requires no locks and produces no garbage at runtime.
/// Adding with <c>immediate = true</c> makes the item visible in the current frame without rebuilding the main
/// snapshot.</para>
/// <para>Typical usage in a game loop:
/// <code>
/// var map = new FastConcurrentIdentifierMap&lt;GameObject&gt;();
/// // at the beginning of the frame
/// map.RefreshSnapshot();
/// // iteration
/// map.ForEachReadOnly(obj => obj.Update(deltaTime));
/// // add with immediate visibility
/// map.TryAdd(newId, newObj, immediate: true);
/// </code>
/// </para>
/// </remarks>
public sealed class FastConcurrentIdentifierMap<T>
    : IEnumerable<T>
{
    private readonly Dictionary<IIdentifier, T> _dict;
    private readonly ReaderWriterLockSlim _lock = new();

    // Both arrays are replaced, never mutated: a reader that grabbed one iterates it with no lock.
    private volatile T[] _snapshot;
    private volatile T[] _hotItems = [];

    /// <summary>
    /// Creates the map. Initial data is visible in the snapshot right away.
    /// </summary>
    /// <param name="data">The initial data.</param>
    public FastConcurrentIdentifierMap(IEnumerable<KeyValuePair<IIdentifier, T>>? data = null)
    {
        _dict = new Dictionary<IIdentifier, T>(data ?? [], IdentifierComparer.Instance);
        _snapshot = _dict.Values.ToArray();
    }

    /// <summary>
    /// Updates the main snapshot with current dictionary values and clears the hot items list.
    /// </summary>
    /// <remarks>
    /// Call this method once per frame before iteration. The method is thread-safe but acquires a write lock,
    /// so do not call it too frequently.
    /// </remarks>
    public void RefreshSnapshot()
    {
        _lock.EnterWriteLock();
        try
        {
            RebuildSnapshot();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    // Must be called with the write lock held.
    private void RebuildSnapshot()
    {
        var newSnapshot = new T[_dict.Count];
        _dict.Values.CopyTo(newSnapshot, 0);

        _snapshot = newSnapshot;
        _hotItems = [];
    }

    /// <summary>
    /// Returns the value associated with the specified key, or <c>default</c> if the key does not exist.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value, or <c>default</c>.</returns>
    public T? GetValueOrDefault(IIdentifier key)
    {
        return TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>
    /// Attempts to retrieve the value for the specified key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The found value, or <c>default</c> if not found.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(IIdentifier key, out T? value)
    {
        if (key is null)
        {
            value = default;
            return false;
        }

        _lock.EnterReadLock();
        try
        {
            return _dict.TryGetValue(key, out value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Adds an element to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="immediate">
    /// If <c>true</c>, the element becomes visible in the current frame (added to the hot list).
    /// If <c>false</c>, it becomes visible only after the next <see cref="RefreshSnapshot"/> call.
    /// </param>
    /// <returns><c>true</c> if the addition succeeded; <c>false</c> if the key already exists.</returns>
    public bool TryAdd(IIdentifier key, T value, bool immediate = false)
    {
        if (key is null) return false;

        _lock.EnterWriteLock();
        try
        {
            if (!_dict.TryAdd(key, value)) return false;

            if (immediate)
                _hotItems = [.. _hotItems, value];

            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes an element from the dictionary and immediately updates the snapshot.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The removed value, or <c>default</c>.</param>
    /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method rebuilds the snapshot inside the write lock, which may be expensive for bulk removals.
    /// For mass deletions, consider using <see cref="TryRemoveDeferred"/>.
    /// </remarks>
    public bool TryRemove(IIdentifier key, out T? value)
    {
        if (key is null)
        {
            value = default;
            return false;
        }

        _lock.EnterWriteLock();
        try
        {
            if (!_dict.Remove(key, out value)) return false;

            RebuildSnapshot();
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes an element from the dictionary without updating the snapshot.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The removed value, or <c>default</c>.</param>
    /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The element will disappear from iterations only after the next <see cref="RefreshSnapshot"/> call.
    /// Useful for bulk removals to avoid repeated snapshot copies.
    /// </remarks>
    public bool TryRemoveDeferred(IIdentifier key, out T? value)
    {
        if (key is null)
        {
            value = default;
            return false;
        }

        _lock.EnterWriteLock();
        try
        {
            return _dict.Remove(key, out value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Checks whether the dictionary contains the specified key.
    /// </summary>
    public bool ContainsKey(IIdentifier key)
    {
        if (key is null) return false;

        _lock.EnterReadLock();
        try
        {
            return _dict.ContainsKey(key);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the dictionary.
    /// </summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _dict.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Indicates whether the dictionary is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Removes all keys and values from the dictionary and clears the snapshot.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _dict.Clear();
            RebuildSnapshot();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns the current snapshot of all values (without hot items).
    /// </summary>
    /// <returns>An array of values as of the last snapshot rebuild.</returns>
    public T[] GetSnapshot()
    {
        return _snapshot;
    }

    /// <summary>
    /// Executes an action for every element in the dictionary, including hot items.
    /// </summary>
    /// <param name="action">The action to perform on each element.</param>
    /// <remarks>
    /// Iterates first over the snapshot, then over hot items. The iteration is lock-free and thread-safe.
    /// </remarks>
    public void ForEachReadOnly(Action<T> action)
    {
        foreach (var item in _snapshot)
            action(item);

        foreach (var item in _hotItems)
            action(item);
    }

    /// <summary>
    /// Iterates over elements until a predicate returns <c>false</c>.
    /// </summary>
    /// <param name="predicate">A function that receives an element and returns <c>true</c> to continue iteration.</param>
    public void ForEachUntil(Func<T, bool> predicate)
    {
        foreach (var item in _snapshot)
            if (!predicate(item))
                return;

        foreach (var item in _hotItems)
            if (!predicate(item))
                return;
    }

    /// <summary>
    /// Returns an enumerator that iterates only over the main snapshot (excluding hot items).
    /// </summary>
    /// <remarks>Use <see cref="ForEachReadOnly"/> if you need to include hot items.</remarks>
    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_snapshot).GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
