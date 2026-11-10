using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// Implements the <see cref="IRegistrableQuantumObject"/> contract on top of a concrete registry type.
/// </summary>
/// <remarks>
/// <see cref="IRegistrableQuantumObject.Register"/> takes a plain quantum object, so a wrong registry can only be
/// detected at runtime. It is reported instead of being ignored: a silent no-op here means an object that
/// quietly never gets registered.
/// </remarks>
internal static class RegistryBinding
{
    public static void Register<T>(IQuantumObject registry, T item)
        where T : IRegistrableQuantumObject
    {
        Cast<T>(registry).Register(item);
    }

    public static void Unregister<T>(IQuantumObject registry, IIdentifier id)
        where T : IRegistrableQuantumObject
    {
        Cast<T>(registry).Pop(id);
    }

    private static IRegistry<T> Cast<T>(IQuantumObject registry)
        where T : IRegistrableQuantumObject
    {
        ArgumentNullException.ThrowIfNull(registry);

        return registry as IRegistry<T> ?? throw new ArgumentException(
            $"{registry.GetType().Name} cannot store objects of type {typeof(T).Name}", nameof(registry));
    }
}
