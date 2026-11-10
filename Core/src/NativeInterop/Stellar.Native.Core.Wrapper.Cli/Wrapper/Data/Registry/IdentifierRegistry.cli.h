// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Data/Registry/IdentifierRegistry.cli.h
/// @brief The managed face of Stellar::Native::Core::Data::Registry::IdentifierRegistry.
#pragma once

#include "Wrapper/Identifier.cli.h"
#include "Wrapper/Native/CoreContext.cli.h"

namespace Stellar::Core::Data::Registry
{
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace Stellar::Kernel;

    /// <summary>
    /// The registry of identifiers. It holds no state: it is a view of the native registry.
    /// </summary>
    public ref class IdentifierRegistry sealed : Stellar::Kernel::Data::Registry::IRegistry<Identifier^>
    {
        Stellar::Core::Native::CoreContext^ _context;

    internal:
        /// The registry of a context: only the context makes one.
        IdentifierRegistry(Stellar::Core::Native::CoreContext^ context) : _context(context) {}

    public:
        /// <inheritdoc/>
        virtual bool Exists(IIdentifier^ id);

        /// <inheritdoc/>
        virtual bool Register(Identifier^ obj);

        /// <inheritdoc/>
        virtual Identifier^ Get(IIdentifier^ id);

        /// <summary>Finds a registered identifier by its value.</summary>
        Identifier^ Get(Guid uid);

        /// <inheritdoc/>
        virtual Identifier^ Pop(IIdentifier^ id);

        /// <inheritdoc/>
        virtual property int Size { int get(); }

        /// <inheritdoc/>
        virtual property ICollection<IIdentifier^>^ Keys { ICollection<IIdentifier^>^ get(); }

        /// <inheritdoc/>
        virtual property ICollection<Identifier^>^ Values { ICollection<Identifier^>^ get(); }
    };
}
