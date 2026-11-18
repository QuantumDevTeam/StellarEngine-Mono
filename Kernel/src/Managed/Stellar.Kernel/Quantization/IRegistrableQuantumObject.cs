using System;
using Stellar.Kernel.Data.Registry;

namespace Stellar.Kernel.Quantization;

/// <summary>
/// A quantum object that registries can hold.
/// </summary>
/// <remarks>
/// <para>The object does not register itself: a registry takes it (<see cref="IRegistry{T}.Register"/>) and lets it
/// go, and keeps <see cref="Registrations"/> up to date. Disposing the object removes it from every registry it is
/// in.</para>
/// <para>The base classes of Stellar.Core may ask the registries of the current engine to take a new object (implicit
/// registration); it is still the registries that do it, and it can be turned off for a class.</para>
/// </remarks>
public interface IRegistrableQuantumObject
    : IIdentifiableQuantumObject, IDisposable
{
    /// <summary>
    /// Gets the registries this object is in.
    /// </summary>
    IRegistrations Registrations { get; }
}
