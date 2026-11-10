// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Registry/IdentifierRegistry.h
/// @brief The registry of identifiers: the single source of truth for both sides of the engine.
#pragma once

#include <cstddef>
#include <memory>
#include <vector>

#include "Identifier.h"

namespace Stellar::Native::Core::Data::Registry
{
    /// @brief The registry of identifiers of one engine (CoreContext).
    ///
    /// Managed code has no registry of its own: it reaches this one through the wrappers.
    class STELLAR_CORE_API IdentifierRegistry final
    {
        struct Impl;
        UniquePtr<Impl> _impl;

    public:
        /// @brief Creates an empty registry. A CoreContext owns one; tests may make their own.
        IdentifierRegistry();
        ~IdentifierRegistry();

        IdentifierRegistry(const IdentifierRegistry&) = delete;
        IdentifierRegistry& operator=(const IdentifierRegistry&) = delete;

        /// @brief Adds an identifier. @return False if it is registered already or null.
        bool Register(Link<const Identifier> id);

        /// @brief Removes an identifier. @return False if it is not registered.
        bool Unregister(Link<const Identifier> id);

        /// @brief Checks for an identifier.
        [[nodiscard]] bool Contains(Link<const Identifier> id) const;

        /// @brief Gets the number of identifiers.
        [[nodiscard]] std::size_t size() const;

        /// @brief Gets a snapshot of the identifiers.
        [[nodiscard]] std::vector<Identifier> Snapshot() const;
    };
}
