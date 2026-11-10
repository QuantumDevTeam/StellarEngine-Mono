// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/Helpers.h
/// @brief Small type traits and bitwise helpers used by engine enumerations.
#pragma once

#include <type_traits>

#include "Concepts.h"

namespace Stellar::Native
{
    /// @brief Trait that marks an enumeration as a bit-flag enumeration.
    ///
    /// Specialize it in the enumeration's namespace to enable the bitwise
    /// operators and HasFlag for that enum:
    ///
    /// @code
    /// template <> struct IsFlagEnum<MyFlags> : std::true_type {};
    /// @endcode
    ///
    /// @tparam E Enumeration type to classify.
    template <Concepts::Enum E> // NOLINT
    struct IsFlagEnum : std::false_type {};

    /// @brief Convenience variable template for IsFlagEnum.
    /// @tparam E Enumeration type to classify.
    template <Concepts::Enum E>
    inline constexpr bool IsFlagEnumV = IsFlagEnum<E>::value;

    /// @brief Combines two flag values with bitwise OR.
    /// @tparam E Flag enumeration with `IsFlagEnumV<E> == true`.
    /// @param a Left operand.
    /// @param b Right operand.
    /// @return Union of the two flag sets.
    template <Concepts::Enum E> requires IsFlagEnumV<E>
    [[nodiscard]] constexpr E operator|(E a, E b) noexcept
    {
        using U = std::underlying_type_t<E>;
        return static_cast<E>(static_cast<U>(a) | static_cast<U>(b));
    }

    /// @brief Intersects two flag values with bitwise AND.
    /// @tparam E Flag enumeration with `IsFlagEnumV<E> == true`.
    /// @param a Left operand.
    /// @param b Right operand.
    /// @return Flags present in both operands.
    template <Concepts::Enum E> requires IsFlagEnumV<E>
    [[nodiscard]] constexpr E operator&(E a, E b) noexcept
    {
        using U = std::underlying_type_t<E>;
        return static_cast<E>(static_cast<U>(a) & static_cast<U>(b));
    }

    /// @brief Toggles flags with bitwise XOR.
    /// @tparam E Flag enumeration with `IsFlagEnumV<E> == true`.
    /// @param a Left operand.
    /// @param b Right operand.
    /// @return Flags set in exactly one operand.
    template <Concepts::Enum E> requires IsFlagEnumV<E>
    [[nodiscard]] constexpr E operator^(E a, E b) noexcept
    {
        using U = std::underlying_type_t<E>;
        return static_cast<E>(static_cast<U>(a) ^ static_cast<U>(b));
    }

    /// @brief Inverts all flag bits.
    /// @tparam E Flag enumeration with `IsFlagEnumV<E> == true`.
    /// @param a Value to invert.
    /// @return Bitwise complement of the underlying value.
    template <Concepts::Enum E> requires IsFlagEnumV<E>
    [[nodiscard]] constexpr E operator~(E a) noexcept
    {
        using U = std::underlying_type_t<E>;
        return static_cast<E>(~static_cast<U>(a));
    }

    /// @brief Adds flags to a value in place.
    /// @tparam E Flag enumeration with `IsFlagEnumV<E> == true`.
    /// @param a Value to modify.
    /// @param b Flags to add.
    /// @return Reference to @p a after the update.
    template <Concepts::Enum E> requires IsFlagEnumV<E>
    constexpr E& operator|=(E& a, E b) noexcept { return a = a | b; }

    /// @brief Removes flags from a value in place.
    /// @tparam E Flag enumeration with `IsFlagEnumV<E> == true`.
    /// @param a Value to modify.
    /// @param b Flags to keep.
    /// @return Reference to @p a after the update.
    template <Concepts::Enum E> requires IsFlagEnumV<E>
    constexpr E& operator&=(E& a, E b) noexcept { return a = a & b; }

    /// @brief Tests whether every bit of @p bit is set in @p value.
    /// @tparam E Flag enumeration with `IsFlagEnumV<E> == true`.
    /// @param value Flag set to inspect.
    /// @param bit Flags that must all be present.
    /// @return True when all bits of @p bit are set in @p value.
    template <Concepts::Enum E> requires IsFlagEnumV<E>
    [[nodiscard]] constexpr bool HasFlag(E value, E bit) noexcept
    {
        using U = std::underlying_type_t<E>;
        return (static_cast<U>(value) & static_cast<U>(bit)) == static_cast<U>(bit);
    }

    /// @brief Returns the numeric value of any enumeration.
    /// @tparam E Enumeration type.
    /// @param e Enumeration value to convert.
    /// @return Value cast to the enum's underlying integer type.
    template <Concepts::Enum E>
    [[nodiscard]] constexpr auto ToNum(E e) noexcept
    {
        return static_cast<std::underlying_type_t<E>>(e);
    }
}
