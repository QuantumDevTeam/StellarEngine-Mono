using System.Collections;
using Stellar.Core.Data.Collections;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// The registries an object is in, kept by the registries themselves.
/// </summary>
/// <param name="owner">The object.</param>
public sealed class Registrations(IRegistrableQuantumObject owner)
    : IRegistrations
{
    // Replaced on change, never mutated: readers need no lock, a writer swaps it whole
    private IRegistry[] _registries = [];

    /// <inheritdoc/>
    public int Count => Volatile.Read(ref _registries).Length;

    /// <inheritdoc/>
    public bool Contains(IRegistry registry)
    {
        return Array.IndexOf(Volatile.Read(ref _registries), registry) >= 0;
    }

    /// <inheritdoc/>
    public void OnRegistered(IRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        while (true)
        {
            var current = Volatile.Read(ref _registries);
            if (Array.IndexOf(current, registry) >= 0) return;
            if (Interlocked.CompareExchange(ref _registries, [.. current, registry], current) == current) return;
        }
    }

    /// <inheritdoc/>
    public void OnUnregistered(IRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        while (true)
        {
            var current = Volatile.Read(ref _registries);
            var index = Array.IndexOf(current, registry);
            if (index < 0) return;

            IRegistry[] next = current.Length == 1 ? [] : [.. current.AsSpan(0, index), .. current.AsSpan(index + 1)];
            if (Interlocked.CompareExchange(ref _registries, next, current) == current) return;
        }
    }

    /// <inheritdoc/>
    public void UnregisterAll()
    {
        // Each registry tells back, which takes it off this list
        foreach (var registry in Volatile.Read(ref _registries)) registry.Unregister(owner.UID);
    }

    /// <inheritdoc/>
    public SnapshotEnumerator<IRegistry> GetEnumerator()
    {
        return new SnapshotEnumerator<IRegistry>(Volatile.Read(ref _registries));
    }

    IEnumerator<IRegistry> IEnumerable<IRegistry>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
