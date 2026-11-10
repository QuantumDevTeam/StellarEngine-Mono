// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Mixins/Stringable.h
/// @brief Text form of engine types.
#pragma once

#include <concepts>
#include <format>
#include <string>
#include <typeinfo>

namespace Stellar::Native::Core
{
    /// @brief Satisfied by types with a text form.
    template <typename T>
    concept CStringable = requires(const T& obj)
    {
        { obj.ToString() } -> std::convertible_to<std::string>;
    };

    /// @brief Gets the text form of an object.
    template <CStringable T>
    [[nodiscard]] std::string to_string(Link<const T> obj)
    {
        return obj.ToString();
    }

    /// @brief Mixin: the text form of an object is its type and its identifier.
    ///
    /// A type replaces the behaviour simply by declaring its own ToString.
    ///
    /// @tparam Derived The type that inherits the mixin.
    template <typename Derived>
    struct Stringable
    {
        /// @brief Builds the text form from the type name and `GetUID()`.
        [[nodiscard]] std::string ToString() const
        {
            return std::format("{}#UID({})", typeid(Derived).name(),
                               static_cast<Link<const Derived>>(*this).GetUID().ToString());
        }
    };
}
