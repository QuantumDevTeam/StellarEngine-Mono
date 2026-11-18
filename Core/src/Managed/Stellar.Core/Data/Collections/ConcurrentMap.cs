using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// Values by key, safe from any thread: lookups take no lock, enumerations see a snapshot.
/// </summary>
/// <remarks>
/// <para>The snapshot is built once after a change and then shared by every reader. <see cref="Changed"/> is raised
/// after each change, on the thread that made it, with the old value of a replaced key.</para>
/// <para>A derived map brings keys to one form (<see cref="NormalizeKey"/>), refuses what it cannot hold
/// (<see cref="Validate"/>) and says what is missing (<see cref="NotFound"/>): the catalogs of the engine are made
/// this way. A key that cannot be brought to the form has no value: lookups answer no instead of throwing.</para>
/// </remarks>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
public class ConcurrentMap<TKey, TValue>
    : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    // Whether a type of map brings keys to a form: a lookup in one that does not takes no virtual call and no try
    private static readonly ConcurrentDictionary<Type, bool> NormalizingTypes = new();

    private readonly ConcurrentDictionary<TKey, TValue> _items;
    private readonly bool _normalizes;
    private int _version;
    private volatile View? _view;

    /// <summary>
    /// Makes an empty map.
    /// </summary>
    /// <param name="comparer">How keys compare; the default comparer of <typeparamref name="TKey"/> if <c>null</c>.</param>
    public ConcurrentMap(IEqualityComparer<TKey>? comparer = null)
    {
        _items = new ConcurrentDictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
        _normalizes = GetType() != typeof(ConcurrentMap<TKey, TValue>) && NormalizingTypes.GetOrAdd(GetType(),
            static type => type.GetMethod(nameof(NormalizeKey), BindingFlags.Instance | BindingFlags.NonPublic, [typeof(TKey)])!
                .DeclaringType != typeof(ConcurrentMap<TKey, TValue>));
    }

    /// <summary>
    /// Raised after each change, out of any lock.
    /// </summary>
    public event Action<MapChange<TKey, TValue>>? Changed;

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <summary>
    /// Gets whether the map is empty.
    /// </summary>
    public bool IsEmpty => _items.IsEmpty;

    /// <summary>
    /// Gets the pairs as they are now: one array, shared until the map changes. Do not change it.
    /// </summary>
    public IReadOnlyList<KeyValuePair<TKey, TValue>> Snapshot => CurrentView().Pairs;

    /// <summary>
    /// Gets the keys as they are now: one array, shared until the map changes.
    /// </summary>
    public IReadOnlyCollection<TKey> Keys => CurrentView().Keys;

    /// <summary>
    /// Gets the values as they are now: one array, shared until the map changes.
    /// </summary>
    public IReadOnlyCollection<TValue> Values => CurrentView().Values;

    /// <summary>
    /// Gets whether the map has a value for a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> if it has one.</returns>
    public bool ContainsKey(TKey key)
    {
        return TryNormalize(key, out var normalized) && _items.ContainsKey(normalized);
    }

    /// <summary>
    /// Finds the value of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, or <c>default</c>.</param>
    /// <returns><c>true</c> if the map has a value for the key.</returns>
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (TryNormalize(key, out var normalized)) return _items.TryGetValue(normalized, out value);

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the value of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if there is none, or the exception of <see cref="NotFound"/>.</exception>
    public TValue Get(TKey key)
    {
        return TryGet(key, out var value) ? value : throw NotFound(key);
    }

    /// <summary>
    /// Sets the value of a key, replacing the one it had.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Set(TKey key, TValue value)
    {
        var normalized = NormalizeKey(key);
        Validate(normalized, value);

        while (true)
        {
            if (_items.TryGetValue(normalized, out var old))
            {
                if (!_items.TryUpdate(normalized, value, old)) continue;
                OnChanged(new MapChange<TKey, TValue>(ChangeKind.Replaced, normalized, value, old));
                return;
            }

            if (!_items.TryAdd(normalized, value)) continue;
            OnChanged(new MapChange<TKey, TValue>(ChangeKind.Added, normalized, value));
            return;
        }
    }

    /// <summary>
    /// Adds a value for a key that has none.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if it was added; <c>false</c> if the key already has a value, which stays.</returns>
    public bool TryAdd(TKey key, TValue value)
    {
        var normalized = NormalizeKey(key);
        Validate(normalized, value);
        if (!_items.TryAdd(normalized, value)) return false;

        OnChanged(new MapChange<TKey, TValue>(ChangeKind.Added, normalized, value));
        return true;
    }

    /// <summary>
    /// Gets the value of a key, adding one made by a factory if it has none.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="factory">Makes the value; may run even if another thread adds first, then its value is dropped.</param>
    /// <returns>The value of the key.</returns>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var normalized = NormalizeKey(key);
        if (_items.TryGetValue(normalized, out var existing)) return existing;

        var value = factory(normalized);
        Validate(normalized, value);
        return TryAdd(normalized, value) ? value : _items[normalized];
    }

    /// <summary>
    /// Removes the value of a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> if there was one.</returns>
    public bool Remove(TKey key)
    {
        return TryRemove(key, out _);
    }

    /// <summary>
    /// Removes the value of a key and gives it back.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The removed value, or <c>default</c>.</param>
    /// <returns><c>true</c> if there was one.</returns>
    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (!TryNormalize(key, out var normalized) || !_items.TryRemove(normalized, out value))
        {
            value = default;
            return false;
        }

        OnChanged(new MapChange<TKey, TValue>(ChangeKind.Removed, normalized, value));
        return true;
    }

    /// <summary>
    /// Removes every value.
    /// </summary>
    public void Clear()
    {
        if (_items.IsEmpty) return;

        _items.Clear();
        OnChanged(new MapChange<TKey, TValue>(ChangeKind.Cleared, default!, default!));
    }

    /// <summary>
    /// Enumerates the pairs as they were when the enumeration started.
    /// </summary>
    public SnapshotEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return new SnapshotEnumerator<KeyValuePair<TKey, TValue>>(CurrentView().Pairs);
    }

    /// <summary>
    /// Enumerates the values as they were when the enumeration started.
    /// </summary>
    public SnapshotEnumerator<TValue> EnumerateValues()
    {
        return new SnapshotEnumerator<TValue>(CurrentView().Values);
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Brings a key to the form the map stores. The key as it is by default.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The key in its form.</returns>
    /// <exception cref="ArgumentException">Thrown for a key the map cannot hold.</exception>
    protected virtual TKey NormalizeKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key;
    }

    /// <summary>
    /// Refuses a value the map cannot hold under a key. Accepts anything by default.
    /// </summary>
    /// <param name="key">The key, in its form.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentException">Thrown for a value the map cannot hold.</exception>
    protected virtual void Validate(TKey key, TValue value)
    {
    }

    /// <summary>
    /// Makes the exception of <see cref="Get"/> for a key without a value.
    /// </summary>
    /// <param name="key">The key, as it was asked for.</param>
    /// <returns>The exception.</returns>
    protected virtual Exception NotFound(TKey key)
    {
        return new KeyNotFoundException($"Nothing for '{key}'");
    }

    private void OnChanged(MapChange<TKey, TValue> change)
    {
        Interlocked.Increment(ref _version);
        Changed?.Invoke(change);
    }

    // Built after reading the version: a change in between only makes the next reader build again
    private View CurrentView()
    {
        var version = Volatile.Read(ref _version);
        var view = _view;
        if (view is not null && view.Version == version) return view;

        view = new View(version, _items.ToArray());
        _view = view;
        return view;
    }

    // A key that cannot be held has no value: lookups answer no instead of throwing
    private bool TryNormalize(TKey key, [MaybeNullWhen(false)] out TKey normalized)
    {
        if (!_normalizes)
        {
            normalized = key;
            return key is not null;
        }

        try
        {
            normalized = NormalizeKey(key);
            return true;
        }
        catch (ArgumentException)
        {
            normalized = default;
            return false;
        }
    }

    // The map at one version: the pairs, and the keys and the values made from them when first asked for
    private sealed class View(int version, KeyValuePair<TKey, TValue>[] pairs)
    {
        private TKey[]? _keys;
        private TValue[]? _values;

        public int Version { get; } = version;

        public KeyValuePair<TKey, TValue>[] Pairs { get; } = pairs;

        // Two readers may both make them: either array is right
        public TKey[] Keys => _keys ??= Project(Pairs, static pair => pair.Key);

        public TValue[] Values => _values ??= Project(Pairs, static pair => pair.Value);

        private static T[] Project<T>(KeyValuePair<TKey, TValue>[] pairs, Func<KeyValuePair<TKey, TValue>, T> part)
        {
            if (pairs.Length == 0) return [];

            var items = new T[pairs.Length];
            for (var i = 0; i < pairs.Length; i++) items[i] = part(pairs[i]);
            return items;
        }
    }
}
