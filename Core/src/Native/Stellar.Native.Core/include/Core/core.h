// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Core/core.h
/// @brief Handle of the native core: macros and forward declarations. Include it after Kernel/kernel.h.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include "macro.h"

/// @brief Native implementation of the basic concepts of the engine.
namespace Stellar::Native::Core
{
    struct Identifier;
    struct Label;
    class CoreContext;

    namespace Memory
    {
        class Allocator;
    }

    namespace Data::Registry
    {
    }

    namespace Modules
    {
        class ModuleRegistry;
    }
}
