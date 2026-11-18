using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Stellar.Core.Data.Registry;
using Stellar.Core.Quantization;
using Stellar.Kernel.Data.Collections;
using Stellar.Kernel.Data.Registry;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// The base of the containers: items by key, a quant with an identity, always registered, sealable.
/// </summary>
/// <remarks>
/// <para>A new container goes into the registry of containers of the current engine (implicit registration, see
/// <see cref="Stellar.Core.Quantization.RegistrableQuant{T,TMeta}"/>), so it can be found by its identifier from
/// anywhere. Disposing it removes it from every registry it is in.</para>
/// <para>Reads take no lock and enumerations see a snapshot (<see cref="ConcurrentMap{TKey,TValue}"/>);
/// <see cref="Changed"/> tells what changed. <see cref="Seal"/> fixes the contents for good.</para>
/// </remarks>
/// <typeparam name="TKey">The key of an item.</typeparam>
/// <typeparam name="TValue">The item.</typeparam>
public abstract class DataContainer<TKey, TValue>
    : Quant<MetaQuant>, IDataContainer<TKey, TValue>
    where TKey : notnull
{
    private volatile bool _sealed;

    /// <summary>
    /// Makes a container.
    /// </summary>
    /// <param name="meta">Its primary meta; a new one if <c>null</c>.</param>
    /// <param name="comparer">How keys compare; the default comparer of <typeparamref name="TKey"/> if <c>null</c>.</param>
    protected DataContainer(MetaQuant? meta = null, IEqualityComparer<TKey>? comparer = null)
        : base(meta ?? new MetaQuant())
    {
        Items = new ConcurrentMap<TKey, TValue>(comparer);
        Registrations = new Registrations(this);

        // Called before the constructors of derived classes: an override returns a constant
        if (RegistersImplicitly) Global.CoreEngine.Current.Registry.RegisterImplicitly(this, typeof(IDataContainer));
    }

    /// <inheritdoc/>
    public IRegistrations Registrations { get; }

    /// <summary>
    /// Raised after each change of the items, out of any lock.
    /// </summary>
    public event Action<MapChange<TKey, TValue>>? Changed
    {
        add => Items.Changed += value;
        remove => Items.Changed -= value;
    }

    /// <inheritdoc/>
    public int Count => Items.Count;

    /// <inheritdoc/>
    public bool IsEmpty => Items.IsEmpty;

    /// <inheritdoc/>
    public bool IsSealed => _sealed;

    /// <inheritdoc/>
    public IReadOnlyCollection<TKey> Keys => Items.Keys;

    /// <inheritdoc/>
    public IReadOnlyCollection<TValue> Values => Items.Values;

    /// <summary>
    /// Gets the items, by key.
    /// </summary>
    protected ConcurrentMap<TKey, TValue> Items { get; }

    /// <summary>
    /// Gets whether a new container goes into the registries of the current engine by itself. <c>true</c> by default.
    /// </summary>
    protected virtual bool RegistersImplicitly => true;

    /// <inheritdoc/>
    public bool ContainsKey(TKey key)
    {
        return Items.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool TryGet(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return Items.TryGet(key, out value);
    }

    /// <inheritdoc/>
    public TValue Get(TKey key)
    {
        return Items.TryGet(key, out var value)
            ? value
            : throw new KeyNotFoundException($"{key} is not in {GetType().Name} {UID}");
    }

    /// <summary>
    /// Fixes the contents for good: every change from now on throws.
    /// </summary>
    public void Seal()
    {
        _sealed = true;
    }

    /// <summary>
    /// Enumerates the items as they were when the enumeration started.
    /// </summary>
    public SnapshotEnumerator<TValue> GetEnumerator()
    {
        return Items.EnumerateValues();
    }

    IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Removes the container from every registry it is in.
    /// </summary>
    public virtual void Dispose()
    {
        Registrations.UnregisterAll();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{GetType().Name} {UID} ({Count}{(IsSealed ? ", sealed" : "")})";
    }

    /// <summary>
    /// Throws if the container is sealed: called before every change.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if it is.</exception>
    protected void ThrowIfSealed()
    {
        if (_sealed) throw new InvalidOperationException($"{this} is sealed: its contents never change again");
    }
}
