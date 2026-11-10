// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

using System.Text;
using Stellar.Core.Data.Registry;
using Stellar.Core.Native;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core;

/// <summary>
/// The unique identifier of the engine, a managed face of <c>Stellar::Native::Core::Identifier</c>.
/// </summary>
/// <remarks>
/// Native systems address their objects by identifier: managed code passes an identifier instead of marshalling the
/// object. The registry of identifiers exists only on the native side.
/// </remarks>
public sealed unsafe class Identifier
    : IIdentifier, IEquatable<IIdentifier>
{
    /// <summary>
    /// Wraps an existing value.
    /// </summary>
    /// <param name="uid">The value of the identifier.</param>
    public Identifier(Guid uid)
    {
        UID = uid;
    }

    /// <inheritdoc/>
    public Guid UID { get; }

    /// <summary>
    /// Gets a value indicating whether the identifier is in the native registry.
    /// </summary>
    public bool IsRegistered => CoreContext.Current.Identifiers.Exists(this);

    /// <summary>
    /// Registers the identifier in the native registry.
    /// </summary>
    public void Register()
    {
        Register(null);
    }

    /// <inheritdoc/>
    public void Register(IQuantumObject? registry)
    {
        RegistryOf(registry).Register(this);
    }

    /// <summary>
    /// Removes the identifier from the native registry.
    /// </summary>
    public void Unregister()
    {
        Unregister(null);
    }

    /// <inheritdoc/>
    public void Unregister(IQuantumObject? registry)
    {
        RegistryOf(registry).Pop(this);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Unregister();
    }

    /// <inheritdoc/>
    public bool Equals(IIdentifier? other)
    {
        return other is not null && UID == other.UID;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is IIdentifier other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return UID.GetHashCode();
    }

    /// <summary>
    /// Gets the canonical text, formatted by the native side.
    /// </summary>
    public string ToCanonicalString()
    {
        var uid = UID;
        return Exports.ReadText(&uid, &Exports.stellar_identifier_format);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Identifier#{UID}";
    }

    #region Static methods

    /// <summary>
    /// Creates a new random identifier on the native side, without registering it.
    /// </summary>
    public static Identifier Create()
    {
        Guid uid;
        Exports.Check(Exports.stellar_identifier_create(&uid));
        return new Identifier(uid);
    }

    /// <summary>
    /// Creates a new random identifier and registers it.
    /// </summary>
    public static Identifier CreateAndRegister()
    {
        var id = Create();
        id.Register();
        return id;
    }

    /// <summary>
    /// Parses the canonical text on the native side.
    /// </summary>
    /// <returns>The identifier, or <c>null</c> if the text is malformed.</returns>
    public static Identifier? Parse(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        fixed (byte* data = bytes)
        {
            Guid uid;
            int parsed;
            Exports.Check(Exports.stellar_identifier_parse(new Exports.StringView(data, bytes.Length), &uid, &parsed));

            return parsed != 0 ? new Identifier(uid) : null;
        }
    }

    /// <summary>
    /// Finds a registered identifier.
    /// </summary>
    /// <returns>The identifier, or <c>null</c> if it is not registered.</returns>
    public static Identifier? Get(Guid data)
    {
        return CoreContext.Current.Identifiers.Get(data);
    }

    /// <inheritdoc cref="Get(Guid)"/>
    public static Identifier? Get(byte[] data)
    {
        return Get(new Guid(data));
    }

    /// <inheritdoc cref="Get(Guid)"/>
    public static Identifier? Get(string data)
    {
        return Get(new Guid(data));
    }

    /// <inheritdoc cref="Get(Guid)"/>
    public static Identifier? Get(IIdentifier data)
    {
        return Get(data.UID);
    }

    #endregion

    private static IRegistry<Identifier> RegistryOf(IQuantumObject? registry)
    {
        return (registry ?? CoreContext.Current.Identifiers) as IRegistry<Identifier>
               ?? throw new ArgumentException($"{registry!.GetType().Name} cannot store identifiers", nameof(registry));
    }
}
