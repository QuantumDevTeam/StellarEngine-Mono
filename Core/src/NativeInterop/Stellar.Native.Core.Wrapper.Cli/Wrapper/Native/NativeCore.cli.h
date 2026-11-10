// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Native/NativeCore.cli.h
/// @brief Information about the wrapper of the native core.
#pragma once

#include "Wrapper/Interop.cli.h"

namespace Stellar::Core::Native
{
    using namespace System;

    /// <summary>
    /// Information about the wrapper of the native core this process uses.
    /// </summary>
    public ref class NativeCore abstract sealed
    {
    public:
        /// <summary>The name of this wrapper.</summary>
        literal String^ WrapperName = "Cli";

        /// <summary>Gets the name of the wrapper: <c>Managed</c> (P/Invoke) or <c>Cli</c> (C++/CLI).</summary>
        static property String^ Wrapper { String^ get() { return WrapperName; } }

        /// <summary>Gets a value indicating whether the native libraries can be loaded.</summary>
        static property bool IsAvailable { bool get(); }

        /// <summary>Fails if the native libraries are missing.</summary>
        static void EnsureAvailable();
    };
}
