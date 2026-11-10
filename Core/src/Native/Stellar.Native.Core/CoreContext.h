// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file CoreContext.h
/// @brief The registries of one engine.
#pragma once

#include <memory>

#include "Data/Registry/IdentifierRegistry.h"
#include "Data/Registry/LabelRegistry.h"
#include "Data/Registry/ContainerRegistry.h"
#include "Modules/ModuleRegistry.h"

namespace Stellar::Native::Core
{
    /// @brief Everything the native core keeps for one engine: its identifiers, labels, containers and modules.
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

        /// @brief Gets the registry of identifiers.
        [[nodiscard]] Link<Data::Registry::IdentifierRegistry> Identifiers() const;

        /// @brief Gets the registry of labels.
        [[nodiscard]] Link<Data::Registry::LabelRegistry> Labels() const;

        /// @brief Gets the registry of the membership of containers.
        [[nodiscard]] Link<Data::Registry::ContainerRegistry> Containers() const;

        /// @brief Gets the registry of native modules. Destroying the context unloads its modules.
        [[nodiscard]] Link<Modules::ModuleRegistry> Modules() const;
    };
}
