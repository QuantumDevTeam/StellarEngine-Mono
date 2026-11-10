using System.Collections.Concurrent;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// Thread-safe base of the engine registries.
/// </summary>
/// <typeparam name="T">The type of stored objects.</typeparam>
public abstract class ConcurrentRegistry<T>
    : IRegistry<T>
    where T : class, IRegistrableQuantumObject
{
    private readonly ConcurrentDictionary<Guid, T> _items = new();

    /// <summary>
    /// Gets the identifier an object is stored under.
    /// </summary>
    protected abstract IIdentifier IdentifierOf(T obj);

    /// <inheritdoc/>
    public bool Exists(IIdentifier id)
    {
        return id is not null && _items.ContainsKey(id.UID);
    }

    /// <inheritdoc/>
    public virtual bool Register(T obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return _items.TryAdd(IdentifierOf(obj).UID, obj);
    }

    /// <inheritdoc/>
    public T? Get(IIdentifier id)
    {
        return id is null ? null : Get(id.UID);
    }

    /// <summary>
    /// Retrieves a registered object by the value of its identifier.
    /// </summary>
    public T? Get(Guid uid)
    {
        return _items.GetValueOrDefault(uid);
    }

    /// <inheritdoc/>
    public T? Pop(IIdentifier id)
    {
        return id is null ? null : Pop(id.UID);
    }

    /// <summary>
    /// Removes and returns a registered object by the value of its identifier.
    /// </summary>
    public virtual T? Pop(Guid uid)
    {
        return _items.TryRemove(uid, out var obj) ? obj : null;
    }

    /// <inheritdoc/>
    public int Size => _items.Count;

    /// <inheritdoc/>
    public ICollection<IIdentifier> Keys => _items.Values.Select(IdentifierOf).ToArray();

    /// <inheritdoc/>
    public ICollection<T> Values => _items.Values.ToArray();
}
