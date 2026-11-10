// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Data/Registry/LabelRegistry.cli.h
/// @brief The managed face of Stellar::Native::Core::Data::Registry::LabelRegistry.
#pragma once

#include "Wrapper/Label.cli.h"
#include "Wrapper/Native/CoreContext.cli.h"

namespace Stellar::Core::Data::Registry
{
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace Stellar::Kernel;

    /// <summary>
    /// The registry of labels. It holds no state: it is a view of the native registry.
    /// </summary>
    public ref class LabelRegistry sealed : Stellar::Kernel::Data::Registry::IRegistry<Label^>
    {
        Stellar::Core::Native::CoreContext^ _context;

    internal:
        /// The registry of a context: only the context makes one.
        LabelRegistry(Stellar::Core::Native::CoreContext^ context) : _context(context) {}

    public:
        /// <inheritdoc/>
        virtual bool Exists(IIdentifier^ id);

        /// <inheritdoc/>
        virtual bool Register(Label^ obj);

        /// <inheritdoc/>
        virtual Label^ Get(IIdentifier^ id);

        /// <summary>Finds a label by its name.</summary>
        /// <returns>The label, or <c>null</c> if the name is not registered.</returns>
        Label^ GetByName(String^ name);

        /// <inheritdoc/>
        virtual Label^ Pop(IIdentifier^ id);

        /// <inheritdoc/>
        virtual property int Size { int get(); }

        /// <summary>Not supported: the native registry does not enumerate labels.</summary>
        virtual property ICollection<IIdentifier^>^ Keys { ICollection<IIdentifier^>^ get(); }

        /// <summary>Not supported: the native registry does not enumerate labels.</summary>
        virtual property ICollection<Label^>^ Values { ICollection<Label^>^ get(); }
    };
}
