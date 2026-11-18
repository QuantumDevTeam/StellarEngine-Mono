using System;

namespace Stellar.Kernel.Data.Registry;

/// <summary>
/// Gives a type a registry of its own in every engine: the objects of the type and of the types derived from it are
/// registered there implicitly.
/// </summary>
/// <remarks>
/// <para>On a base class or a contract, such as <c>Actor</c> or <c>IUpdatable</c>: a <c>Player</c> derived from
/// <c>Actor</c> is then found in the registry of actors. A type can have several registries this way, one per marked
/// type of its hierarchy.</para>
/// <para>The marked type must be a class or an interface that is an <see cref="Quantization.IRegistrableQuantumObject"/>.
/// The attribute is not inherited: a derived type that wants a registry of its own is marked too.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class StellarRegistryAttribute
    : Attribute
{
}
