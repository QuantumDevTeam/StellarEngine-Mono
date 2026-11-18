using System;
using System.Collections.Generic;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Data.Registry;

/// <summary>
/// A registry, whatever it holds: objects of the engine by their identifiers.
/// </summary>
/// <remarks>
/// <para>A registry is the only one that adds an object to itself and removes it, and it tells the object each time
/// (<see cref="IRegistrableQuantumObject.Registrations"/>), so the object knows where it is and leaves every registry
/// when it is disposed. An object never registers itself.</para>
/// <para>This part of the contract needs no type: what lists, finds or removes by identifier.</para>
/// </remarks>
public interface IRegistry
    : IQuantumObject
{
    /// <summary>
    /// Gets the type of the objects of this registry.
    /// </summary>
    Type ItemType { get; }

    /// <summary>
    /// Gets the number of objects registered now.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets a snapshot of the identifiers of the objects registered now.
    /// </summary>
    ICollection<Identifier> Keys { get; }

    /// <summary>
    /// Gets whether an object with the identifier is registered.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns><c>true</c> if it is.</returns>
    bool Exists(Identifier id);

    /// <summary>
    /// Removes the object with the identifier, and tells it.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns><c>true</c> if an object was removed.</returns>
    bool Unregister(Identifier id);
}

/// <summary>
/// Objects of the engine of one type, by their identifiers.
/// </summary>
/// <typeparam name="T">
/// The type of the objects: a base class or a contract, so a <c>Player</c> derived from <c>Actor</c> sits in the
/// registry of actors, and is found there as an actor or, asked so, as a player.
/// </typeparam>
/// <example>
/// <code>
/// actors.Register(player);
/// Actor? actor = actors.Get(player.UID);
/// Player? same = actors.Get&lt;Player&gt;(player.UID);
/// </code>
/// </example>
public interface IRegistry<T>
    : IRegistry
    where T : class, IRegistrableQuantumObject
{
    /// <summary>
    /// Registers an object, and tells it.
    /// </summary>
    /// <param name="item">The object.</param>
    /// <returns><c>true</c> if it was registered; <c>false</c> if an object with its identifier already is.</returns>
    bool Register(T item);

    /// <summary>
    /// Finds an object.
    /// </summary>
    /// <param name="id">Its identifier.</param>
    /// <returns>The object, or <c>null</c> if none is registered.</returns>
    T? Get(Identifier id);

    /// <summary>
    /// Finds an object of a derived type: a checked cast, never a guess.
    /// </summary>
    /// <typeparam name="TItem">The type the object is expected to be.</typeparam>
    /// <param name="id">Its identifier.</param>
    /// <returns>The object, or <c>null</c> if none is registered or it is not a <typeparamref name="TItem"/>.</returns>
    TItem? Get<TItem>(Identifier id)
        where TItem : class, T;

    /// <summary>
    /// Removes an object, and tells it.
    /// </summary>
    /// <param name="id">Its identifier.</param>
    /// <returns>The removed object, or <c>null</c> if none was registered.</returns>
    T? Pop(Identifier id);

    /// <summary>
    /// Gets a snapshot of the objects registered now.
    /// </summary>
    ICollection<T> Values { get; }
}
