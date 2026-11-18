// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file CoreContext.h
/// @brief The registries of one engine.
#pragma once

#include <memory>

#include "Memory/Allocator.h"
#include "Modules/ModuleRegistry.h"

namespace Stellar::Native::Core
{
    /// @brief Everything the native core keeps for one engine: its allocator and its modules.
    ///
    /// Engines are objects, not the process: an editor and the game it runs each have a context, and destroying one
    /// takes its registries with it without touching the other. The default context serves code that runs outside
    /// any engine (tools, tests) and the wrappers when they are given no context.
    class STELLAR_CORE_API CoreContext final
    {
        struct Impl;
        UniquePtr<Impl> _impl;

    public:
        CoreContext();
        ~CoreContext();

        CoreContext(const CoreContext&) = delete;
        CoreContext& operator=(const CoreContext&) = delete;

        /// @brief Gets the default context of the process.
        [[nodiscard]] static Link<CoreContext> Default();

        /// @brief Gets the allocator of the engine. It outlives the context while blocks it gave are not freed.
        [[nodiscard]] Link<Memory::Allocator> Allocator() const;

        /// @brief Gets the registry of native modules. Destroying the context unloads its modules.
        [[nodiscard]] Link<Modules::ModuleRegistry> Modules() const;
    };
}
