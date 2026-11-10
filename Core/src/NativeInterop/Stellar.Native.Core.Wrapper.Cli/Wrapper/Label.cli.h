// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Label.cli.h
/// @brief The managed face of Stellar::Native::Core::Label.
#pragma once

#include "Wrapper/Identifier.cli.h"

namespace Stellar::Core
{
    using namespace System;
    using namespace Stellar::Kernel;
    using namespace Stellar::Kernel::Quantization;

    /// <summary>
    /// A human-readable name bound to an identifier, a managed face of <c>Stellar::Native::Core::Label</c>.
    /// </summary>
    /// <remarks>
    /// A label is a value: two labels with the same identifier and name are equal. The registry of labels exists only
    /// on the native side, which owns the names.
    /// </remarks>
    public ref class Label sealed : ILabel, IEquatable<Label^>
    {
        Identifier^ _uid;
        String^ _name;

        Label(Identifier^ identifier, String^ name);

    internal:
        static Label^ FromNative(Guid identifier, String^ name);

    public:
        /// <inheritdoc/>
        virtual property IIdentifier^ UID { IIdentifier^ get(); }

        /// <inheritdoc/>
        virtual property String^ Name { String^ get(); }

        /// <summary>Registers the label in the native registry.</summary>
        void Register();

        /// <inheritdoc/>
        virtual void Register(IQuantumObject^ registry);

        /// <summary>Removes the label from the native registry.</summary>
        void Unregister();

        /// <inheritdoc/>
        virtual void Unregister(IQuantumObject^ registry);

        /// <inheritdoc/>
        virtual ~Label();

        /// <inheritdoc/>
        virtual bool Equals(Label^ other);

        /// <inheritdoc/>
        bool Equals(Object^ obj) override;

        /// <inheritdoc/>
        int GetHashCode() override;

        /// <inheritdoc/>
        String^ ToString() override;

        // Overloads, not optional parameters: C++/CLI does not write default values to metadata, so C# would pass
        // false for an omitted bool. Both wrappers declare exactly these four.

        /// <summary>Creates a label bound to a new identifier and registers it.</summary>
        /// <returns>The label, or <c>null</c> if the name is blank or taken.</returns>
        static Label^ Create(String^ name);

        /// <summary>Creates a label bound to an identifier and registers it.</summary>
        /// <returns>The label, or <c>null</c> if the name is blank or taken.</returns>
        static Label^ Create(String^ name, IIdentifier^ identifier);

        /// <summary>Creates a label bound to a new identifier.</summary>
        /// <returns>The label, or <c>null</c> if the name is blank or registration found the name already taken.</returns>
        static Label^ Create(String^ name, bool shouldRegister);

        /// <summary>Creates a label.</summary>
        /// <param name="name">The human-readable name.</param>
        /// <param name="identifier">The identifier to bind the label to. A new one is created when null.</param>
        /// <param name="shouldRegister">Whether to put the label to the registry.</param>
        /// <returns>The label, or <c>null</c> if the name is blank or registration found the name already taken.</returns>
        static Label^ Create(String^ name, IIdentifier^ identifier, bool shouldRegister);

        /// <summary>Finds a registered label by the identifier it is bound to.</summary>
        static Label^ Get(IIdentifier^ identifier);

        /// <summary>Finds a registered label by its name.</summary>
        static Label^ Get(String^ name);

        /// <summary>Finds the label with this name, creating it on first use.</summary>
        static operator Label^(String^ name);
    };
}
