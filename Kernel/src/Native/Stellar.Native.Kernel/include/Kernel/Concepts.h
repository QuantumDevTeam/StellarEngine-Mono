// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/Concepts.h
/// @brief Fundamental type concepts shared by every kernel subsystem.
#pragma once

#include <type_traits>

/// @brief General-purpose type concepts used across the engine.
namespace Stellar::Native::Concepts
{
    /// @brief Satisfied by arithmetic types (integers, floating point, bool, char).
    ///
    /// @tparam T Type to test.
    template <typename T>
    concept Scalar = std::is_arithmetic_v<T>;

    /// @brief Satisfied by enumeration types.
    ///
    /// @tparam T Type to test.
    template <typename T>
    concept Enum = std::is_enum_v<T>;

    /// @brief Satisfied by types that expose a `getNativeHandle()` method.
    ///
    /// The returned handle must be convertible to `void*`; this is the contract
    /// checked by must_provide_native_handle.
    ///
    /// @tparam D Type to test.
    template <typename D>
    concept ProvidesNativeHandle = requires(const D& d)
    {
        { d.getNativeHandle() } -> std::convertible_to<void*>;
    };
}
