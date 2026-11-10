using System.Collections;
using Stellar.Core.Data.Registry;
using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Data.Collections;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Collections;

/// <inheritdoc cref="IDataContainer"/>
/// <typeparam name="T">Type of stored data.</typeparam>
/// <remarks>
/// <para>The membership lives on the native side, in <see cref="ContainerRegistry"/>, under the identifier of this
/// container: native systems walk a container without calling managed code. This class keeps only the managed objects,
/// so that it can hand them out.</para>
/// <para>The native side is the source of truth: an item is stored here only after the native container accepted it.</para>
/// </remarks>
public abstract class DataContainer<T>
    : Quant<MetaQuant>, IDataContainer, IEnumerable<T>
    where T : IIdentifiableQuantumObject
{
    /// <summary>
    /// Gets the managed objects of the container.
    /// </summary>
    protected FastConcurrentIdentifierMap<T> Data { get; }

    /// <summary>
    /// Gets the native containers.
    /// </summary>
    protected ContainerRegistry Native { get; } = Global.CoreEngine.Current.Native.Containers;

    // The registry of data containers of the engine the container was made in
    private readonly IQuantumObject _home = Global.CoreEngine.Current.Registry.DataContainers;

    #region Constructors

    /// <summary>
    /// Creates a container by using meta and typed data.
    /// </summary>
    /// <param name="meta">A meta quant of this container.</param>
    /// <param name="data">Initial data for this container.</param>
    protected DataContainer(MetaQuant meta, FastConcurrentIdentifierMap<T> data)
        : base(meta)
    {
        ArgumentNullException.ThrowIfNull(data);

        Data = data;
        Native.Create(UID.UID);

        foreach (var item in data.GetSnapshot())
            Native.Add(UID.UID, item.UID.UID);

        if (IsReadOnly)
            Native.Seal(UID.UID);

        Register(_home);
    }

    /// <summary>
    /// Creates a container by using meta and data stored in a dictionary.
    /// </summary>
    /// <param name="meta">A meta quant of this container.</param>
    /// <param name="data">Initial data stored in a dictionary.</param>
    protected DataContainer(MetaQuant meta, Dictionary<IIdentifier, T> data)
        : this(meta, new FastConcurrentIdentifierMap<T>(data))
    {
    }

    /// <summary>
    /// Creates a container by using meta only.
    /// </summary>
    /// <param name="meta">A meta quant of this container.</param>
    protected DataContainer(MetaQuant meta)
        : this(meta, new FastConcurrentIdentifierMap<T>())
    {
    }

    #endregion

    /// <summary>
    /// Gets a value indicating whether the content is fixed at creation.
    /// </summary>
    /// <remarks>Read by the constructor of this class: an override must not depend on the state of the derived class.</remarks>
    protected virtual bool IsReadOnly => true;

    /// <summary>
    /// Gets a container by its identifier.
    /// </summary>
    /// <param name="identifier">The identifier of the container.</param>
    /// <returns>The container, or <c>null</c> if it is not registered.</returns>
    public static IDataContainer? GetContainer(IIdentifier identifier)
    {
        return Global.CoreEngine.Current.Registry.DataContainers.Get(identifier);
    }

    #region Item support

    /// <inheritdoc/>
    public abstract IIdentifiableQuantumObject? Get(IIdentifier identifier);

    /// <summary>
    /// Stores a quantum object by its identifier.
    /// </summary>
    /// <param name="obj">The object to store.</param>
    /// <returns><c>true</c> if the object was stored; otherwise, <c>false</c>.</returns>
    public virtual bool Set(IIdentifiableQuantumObject obj)
    {
        return false;
    }

    /// <summary>
    /// Attempts to remove and return the object stored in the container.
    /// </summary>
    /// <param name="id">The unique identifier of the object.</param>
    /// <returns>The object if found; otherwise, <c>default</c>.</returns>
    public virtual T? Pop(IIdentifier id)
    {
        return default;
    }

    /// <summary>
    /// Base item operations with the container.
    /// </summary>
    /// <param name="key">The unique identifier of the object.</param>
    /// <exception cref="KeyNotFoundException">Thrown if <paramref name="key"/> is not found in the container.</exception>
    public T this[IIdentifier key]
    {
        get => Get(key) is T value ? value : throw new KeyNotFoundException($"{key} is not in the container");
        set => Set(value);
    }

    #endregion

    #region Encapsulation

    /// <summary>
    /// Updates the main snapshot with current values and clears the hot items list.
    /// </summary>
    /// <remarks>
    /// Call this method once per frame before iteration. The method is thread-safe but acquires a write lock,
    /// so do not call it too frequently.
    /// </remarks>
    public void RefreshSnapshot()
    {
        Data.RefreshSnapshot();
    }

    /// <inheritdoc/>
    public bool ContainsKey(IIdentifier key)
    {
        return key is not null && Native.Contains(UID.UID, key.UID);
    }

    /// <inheritdoc/>
    public bool Contains(IIdentifiableQuantumObject obj)
    {
        return ContainsKey(obj.UID);
    }

    /// <inheritdoc/>
    public int Count => Native.Count(UID.UID);

    /// <inheritdoc/>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Gets the values of the last snapshot.
    /// </summary>
    public T[] Values => Data.GetSnapshot();

    /// <summary>
    /// Gets the identifiers of the items, as the native side knows them. This is what is passed to native systems.
    /// </summary>
    public Guid[] Identifiers => Native.Items(UID.UID);

    /// <summary>
    /// Clears the container.
    /// </summary>
    /// <returns><c>false</c> if the container is read-only.</returns>
    public bool Clear()
    {
        if (!Native.Clear(UID.UID))
            return false;

        Data.Clear();
        return true;
    }

    /// <summary>
    /// Executes an action for every element in the container, including hot items.
    /// </summary>
    /// <param name="action">The action to perform on each element.</param>
    /// <remarks>
    /// Iterates first over the snapshot, then over hot items. The iteration is lock-free and thread-safe.
    /// </remarks>
    public void ForEachReadOnly(Action<T> action)
    {
        Data.ForEachReadOnly(action);
    }

    /// <summary>
    /// Iterates over elements until a predicate returns <c>false</c>.
    /// </summary>
    /// <param name="predicate">A function that receives an element and returns <c>true</c> to continue iteration.</param>
    public void ForEachUntil(Func<T, bool> predicate)
    {
        Data.ForEachUntil(predicate);
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        return Data.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    /// <inheritdoc/>
    public void Register(IQuantumObject? registry = null)
    {
        RegistryBinding.Register<IDataContainer>(registry ?? _home, this);
    }

    /// <inheritdoc/>
    public void Unregister(IQuantumObject? registry = null)
    {
        RegistryBinding.Unregister<IDataContainer>(registry ?? _home, UID);
    }

    /// <summary>
    /// Unregisters the container and destroys its native side.
    /// </summary>
    public void Dispose()
    {
        Unregister();
        Native.Destroy(UID.UID);
    }
}
