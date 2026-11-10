// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Data/Registry/ContainerRegistry.cli.h
/// @brief The managed face of Stellar::Native::Core::Data::Registry::ContainerRegistry.
#pragma once

#include "Wrapper/Interop.cli.h"
#include "Wrapper/Native/CoreContext.cli.h"

namespace Stellar::Core::Data::Registry
{
    using namespace System;

    /// <summary>
    /// The native containers of identifiers: what belongs to a DataContainer or a Roster is known on the native side, so
    /// native systems walk containers without calling managed code.
    /// </summary>
    /// <remarks>
    /// Operations on a container that does not exist throw <see cref="Kernel.Interop.NativeCoreException"/> with
    /// <see cref="Kernel.Interop.NativeResult.NotFound"/>.
    /// </remarks>
    public ref class ContainerRegistry sealed
    {
        Stellar::Core::Native::CoreContext^ _context;

    internal:
        /// The registry of a context: only the context makes one.
        ContainerRegistry(Stellar::Core::Native::CoreContext^ context) : _context(context) {}

    public:
        /// <summary>Creates an empty container.</summary>
        /// <returns><c>false</c> if it exists already.</returns>
        bool Create(Guid container);

        /// <summary>Destroys a container.</summary>
        /// <returns><c>false</c> if it does not exist.</returns>
        bool Destroy(Guid container);

        /// <summary>Checks whether a container exists.</summary>
        bool Exists(Guid container);

        /// <summary>Makes a container read-only, for good.</summary>
        void Seal(Guid container);

        /// <summary>Checks whether a container is read-only.</summary>
        bool IsSealed(Guid container);

        /// <summary>Adds an item.</summary>
        /// <returns><c>false</c> if it is there already or the container is sealed.</returns>
        bool Add(Guid container, Guid item);

        /// <summary>Removes an item.</summary>
        /// <returns><c>false</c> if it is not there or the container is sealed.</returns>
        bool Remove(Guid container, Guid item);

        /// <summary>Checks for an item.</summary>
        bool Contains(Guid container, Guid item);

        /// <summary>Gets the number of items.</summary>
        int Count(Guid container);

        /// <summary>Gets a snapshot of the items.</summary>
        array<Guid>^ Items(Guid container);

        /// <summary>Removes every item.</summary>
        /// <returns><c>false</c> if the container is sealed.</returns>
        bool Clear(Guid container);

        /// <summary>Nests a container. The branch must exist.</summary>
        /// <returns><c>false</c> if it is nested already or is the container itself.</returns>
        bool AddBranch(Guid container, Guid branch);

        /// <summary>Removes a nested container.</summary>
        /// <returns><c>false</c> if it is not nested.</returns>
        bool RemoveBranch(Guid container, Guid branch);

        /// <summary>Checks for a nested container.</summary>
        bool ContainsBranch(Guid container, Guid branch);

        /// <summary>Gets a snapshot of the nested containers.</summary>
        array<Guid>^ Branches(Guid container);
    };
}
