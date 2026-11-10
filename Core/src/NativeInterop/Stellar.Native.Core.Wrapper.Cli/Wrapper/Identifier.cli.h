// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Identifier.cli.h
/// @brief The managed face of Stellar::Native::Core::Identifier.
#pragma once

#include "Wrapper/Interop.cli.h"

namespace Stellar::Core
{
    using namespace System;
    using namespace Stellar::Kernel;
    using namespace Stellar::Kernel::Quantization;

    /// <summary>
    /// The unique identifier of the engine, a managed face of <c>Stellar::Native::Core::Identifier</c>.
    /// </summary>
    /// <remarks>
    /// Native systems address their objects by identifier: managed code passes an identifier instead of marshalling the
    /// object. The registry of identifiers exists only on the native side.
    /// </remarks>
    public ref class Identifier sealed : IIdentifier, IEquatable<IIdentifier^>
    {
        Guid _uid;

    public:
        /// <summary>Wraps an existing value.</summary>
        Identifier(Guid uid);

        /// <inheritdoc/>
        virtual property Guid UID { Guid get(); }

        /// <summary>Gets a value indicating whether the identifier is in the native registry.</summary>
        property bool IsRegistered { bool get(); }

        /// <summary>Registers the identifier in the native registry.</summary>
        void Register();

        /// <inheritdoc/>
        virtual void Register(IQuantumObject^ registry);

        /// <summary>Removes the identifier from the native registry.</summary>
        void Unregister();

        /// <inheritdoc/>
        virtual void Unregister(IQuantumObject^ registry);

        /// <inheritdoc/>
        virtual ~Identifier();

        /// <inheritdoc/>
        virtual bool Equals(IIdentifier^ other);

        /// <inheritdoc/>
        bool Equals(Object^ obj) override;

        /// <inheritdoc/>
        int GetHashCode() override;

        /// <summary>Gets the canonical text, formatted by the native side.</summary>
        String^ ToCanonicalString();

        /// <inheritdoc/>
        String^ ToString() override;

        /// <summary>Creates a new random identifier on the native side, without registering it.</summary>
        static Identifier^ Create();

        /// <summary>Creates a new random identifier and registers it.</summary>
        static Identifier^ CreateAndRegister();

        /// <summary>Parses the canonical text on the native side.</summary>
        /// <returns>The identifier, or <c>null</c> if the text is malformed.</returns>
        static Identifier^ Parse(String^ text);

        /// <summary>Finds a registered identifier.</summary>
        /// <returns>The identifier, or <c>null</c> if it is not registered.</returns>
        static Identifier^ Get(Guid data);

        /// <inheritdoc cref="Get(Guid)"/>
        static Identifier^ Get(array<Byte>^ data);

        /// <inheritdoc cref="Get(Guid)"/>
        static Identifier^ Get(String^ data);

        /// <inheritdoc cref="Get(Guid)"/>
        static Identifier^ Get(IIdentifier^ data);
    };
}
