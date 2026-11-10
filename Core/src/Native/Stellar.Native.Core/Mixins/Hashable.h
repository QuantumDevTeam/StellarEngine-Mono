// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Mixins/Hashable.h
/// @brief Hashing of engine types.
///
/// Public headers of the core stay C++20: C++/CLI wrappers include them directly.
#pragma once

#include <concepts>
#include <cstddef>
#include <cstdint>
#include <functional>

namespace Stellar::Native::Core
{
    /// @brief Satisfied by types that compute their own hash code.
    template <typename T>
    concept CHashable = requires(const T& obj)
    {
        { obj.GetHashCode() } -> std::convertible_to<uint64_t>;
    };

    /// @brief Mixin: the hash code of an object is the hash code of its identifier.
    ///
    /// Static polymorphism, no virtual functions: the mixin adds no vtable, so value types keep their layout.
    /// A type replaces the behaviour simply by declaring its own GetHashCode.
    ///
    /// @tparam Derived The type that inherits the mixin.
    template <typename Derived>
    struct Hashable
    {
        /// @brief Computes the hash code from `GetUID()`.
        [[nodiscard]] uint64_t GetHashCode() const noexcept
        {
            return static_cast<Link<const Derived>>(*this).GetUID().GetHashCode();
        }
    };
}

/// @brief Makes every engine hashable type usable in standard containers.
template <Stellar::Native::Core::CHashable T>
struct std::hash<T> // NOLINT(cert-dcl58-cpp)
{
    [[nodiscard]] size_t operator()(Link<const T> obj) const noexcept
    {
        return static_cast<size_t>(obj.GetHashCode());
    }
};
