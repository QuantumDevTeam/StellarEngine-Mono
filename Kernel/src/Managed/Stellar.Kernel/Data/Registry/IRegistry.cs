using System.Collections.Generic;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Data.Registry;

/// <summary>
/// Provides a storage and lookup service for registrable quantum objects.
/// </summary>
/// <typeparam name="T">The type of objects stored in the registry. Must implement <see cref="IRegistrableQuantumObject"/>.</typeparam>
/// <remarks>
/// <para>The registry maps <see cref="IIdentifier.UID"/> to object instances. It is used for global or scoped access
/// to engine components such as entities, tasks, resources, and systems.</para>
/// <para>Implementations must be thread-safe.</para>
/// </remarks>
/// <example>
/// Registering and retrieving an object:
/// <code>
/// IRegistry&lt;IMyObject&gt; registry = new MyRegistry&lt;IMyObject&gt;();
/// IMyObject obj = ...;
/// registry.Register(obj);
/// IMyObject? found = registry.Get(obj.UID);
/// </code>
/// </example>
public interface IRegistry<T>
    : IQuantumObject
    where T : IRegistrableQuantumObject
{
    /// <summary>
    /// Determines whether an object with the specified identifier exists in the registry.
    /// </summary>
    /// <param name="id">The unique identifier of the object.</param>
    /// <returns><c>true</c> if the object exists; otherwise, <c>false</c>.</returns>
    bool Exists(IIdentifier id);

    /// <summary>
    /// Registers an object in the registry.
    /// </summary>
    /// <param name="obj">The object to register.</param>
    /// <returns><c>true</c> if the object was registered; <c>false</c> if an object with the same key already exists.</returns>
    bool Register(T obj);

    /// <summary>
    /// Retrieves a registered object by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the object.</param>
    /// <returns>The object if found; otherwise, <c>null</c>.</returns>
    T? Get(IIdentifier id);

    /// <summary>
    /// Removes and returns a registered object by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the object.</param>
    /// <returns>The removed object, or <c>null</c> if the object was not registered.</returns>
    T? Pop(IIdentifier id);

    /// <summary>
    /// Gets the number of objects currently registered.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets a snapshot of the identifiers of all registered objects.
    /// </summary>
    ICollection<IIdentifier> Keys { get; }

    /// <summary>
    /// Gets a snapshot of all registered objects.
    /// </summary>
    ICollection<T> Values { get; }
}
