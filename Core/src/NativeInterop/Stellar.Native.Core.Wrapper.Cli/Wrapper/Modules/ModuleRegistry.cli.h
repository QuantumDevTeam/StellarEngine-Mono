// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Modules/ModuleRegistry.cli.h
/// @brief The managed face of Stellar::Native::Core::Modules::ModuleRegistry.
#pragma once

#include "Wrapper/Interop.cli.h"
#include "Wrapper/Native/CoreContext.cli.h"

namespace Stellar::Core::Modules
{
    using namespace System;
    using Stellar::Kernel::Interop::NativeHandle;

    /// <summary>
    /// The loaded native modules of the process.
    /// </summary>
    public ref class ModuleRegistry sealed
    {
        Stellar::Core::Native::CoreContext^ _context;

    internal:
        /// The registry of a context: only the context makes one.
        ModuleRegistry(Stellar::Core::Native::CoreContext^ context) : _context(context) {}

    public:
        /// <summary>Gets the number of loaded modules.</summary>
        property int Count { int get(); }

        /// <summary>Loads a native module, checks its contract and initializes it.</summary>
        /// <param name="path">The path of the shared library.</param>
        /// <returns>The handle of the module.</returns>
        NativeHandle Load(String^ path);

        /// <summary>Shuts a native module down and unloads it.</summary>
        /// <returns><c>false</c> if the handle is stale.</returns>
        bool Unload(NativeHandle module);

        /// <summary>Gets the name of a loaded module.</summary>
        String^ GetName(NativeHandle module);
    };
}
