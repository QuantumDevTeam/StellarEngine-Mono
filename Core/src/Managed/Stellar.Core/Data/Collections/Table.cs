using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// Objects of the engine by their identifiers: a container of the world.
/// </summary>
/// <remarks>
/// A table is found in the registry of containers of its engine, and finds its objects as what they are:
/// <c>table.Get&lt;Player&gt;(id)</c> is a checked cast, <c>null</c> when the object is not a player.
/// </remarks>
/// <typeparam name="T">The type of the objects: a base class or a contract.</typeparam>
public class Table<T>
    : DataContainer<Identifier, T>
    where T : class, IIdentifiableQuantumObject
{
    /// <summary>
    /// Makes a table.
    /// </summary>
    /// <param name="items">The first objects; none if <c>null</c>.</param>
    /// <param name="meta">Its primary meta; a new one if <c>null</c>.</param>
    public Table(IEnumerable<T>? items = null, MetaQuant? meta = null)
        : base(meta)
    {
        if (items is null) return;
        foreach (var item in items) Add(item);
    }

    /// <summary>
    /// Finds an object of a derived type.
    /// </summary>
    /// <typeparam name="TItem">The type the object is expected to be.</typeparam>
    /// <param name="id">Its identifier.</param>
    /// <returns>The object, or <c>null</c> if there is none or it is not a <typeparamref name="TItem"/>.</returns>
    public TItem? Get<TItem>(Identifier id)
        where TItem : class, T
    {
        return Items.TryGet(id, out var item) ? item as TItem : null;
    }

    /// <summary>
    /// Adds an object.
    /// </summary>
    /// <param name="item">The object.</param>
    /// <returns><c>true</c> if it was added; <c>false</c> if an object with its identifier is there.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the table is sealed.</exception>
    public bool Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ThrowIfSealed();
        return Items.TryAdd(item.UID, item);
    }

    /// <summary>
    /// Puts an object in, replacing the one with its identifier.
    /// </summary>
    /// <param name="item">The object.</param>
    /// <exception cref="InvalidOperationException">Thrown if the table is sealed.</exception>
    public void Set(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ThrowIfSealed();
        Items.Set(item.UID, item);
    }

    /// <summary>
    /// Removes an object.
    /// </summary>
    /// <param name="id">Its identifier.</param>
    /// <returns>The removed object, or <c>null</c> if there was none.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the table is sealed.</exception>
    public T? Remove(Identifier id)
    {
        ThrowIfSealed();
        return Items.TryRemove(id, out var item) ? item : null;
    }

    /// <summary>
    /// Removes every object.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the table is sealed.</exception>
    public void Clear()
    {
        ThrowIfSealed();
        Items.Clear();
    }
}
