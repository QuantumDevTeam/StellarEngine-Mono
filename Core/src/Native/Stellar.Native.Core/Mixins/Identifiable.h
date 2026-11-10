// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Mixins/Identifiable.h
/// @brief Objects that have an identifier.
#pragma once

#include <concepts>

#include "Identifier.h"

namespace Stellar::Native::Core
{
    /// @brief Satisfied by types that have an identifier.
    template <typename T>
    concept CIdentifiable = requires(const T& obj)
    {
        { obj.GetUID() } -> std::convertible_to<const Identifier&>;
    };

    /// @brief Mixin: holds the identifier of an object.
    struct Identifiable
    {
    protected:
        Identifier _uid;

        constexpr Identifiable() noexcept = default;

        constexpr explicit Identifiable(Link<const Identifier> uid) noexcept :
            _uid(uid) {}

    public:
        /// @brief Gets the identifier.
        [[nodiscard]] constexpr Link<const Identifier> GetUID() const noexcept { return _uid; }
    };
}
