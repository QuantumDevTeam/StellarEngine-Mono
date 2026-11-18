using System.Collections.Generic;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.Data.Registry;

/// <summary>
/// The registries an object is in: its own record, kept by the registries.
/// </summary>
/// <remarks>
/// <para>A registry calls <see cref="OnRegistered"/> when it takes the object and <see cref="OnUnregistered"/> when
/// it lets it go; nothing else changes this record. So the object leaves the registries it is really in, of whatever
/// engine, when it is disposed (<see cref="UnregisterAll"/>), and anyone can see where it is.</para>
/// </remarks>
public interface IRegistrations
    : IReadOnlyCollection<IRegistry>
{
    /// <summary>
    /// Gets whether the object is in a registry.
    /// </summary>
    /// <param name="registry">The registry.</param>
    /// <returns><c>true</c> if it is.</returns>
    bool Contains(IRegistry registry);

    /// <summary>
    /// Records that a registry took the object. Called by the registry.
    /// </summary>
    /// <param name="registry">The registry.</param>
    void OnRegistered(IRegistry registry);

    /// <summary>
    /// Records that a registry let the object go. Called by the registry.
    /// </summary>
    /// <param name="registry">The registry.</param>
    void OnUnregistered(IRegistry registry);

    /// <summary>
    /// Removes the object from every registry it is in.
    /// </summary>
    void UnregisterAll();
}
