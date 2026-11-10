// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Label.h
/// @brief A human-readable name bound to an identifier.
#pragma once

#include <cstdint>
#include <string>
#include <string_view>

#include "Identifier.h"
#include "Mixins/Hashable.h"
#include "Mixins/Identifiable.h"
#include "Mixins/Stringable.h"

namespace Stellar::Native::Core
{
    /// @brief A human-readable name bound to an identifier.
    ///
    /// A label is a cheap value and does not own its name: the text must outlive the label. Literals are safe, and
    /// Data::Registry::LabelRegistry keeps its own copy of the names of the labels it returns.
    struct STELLAR_EMPTY_BASES STELLAR_CORE_API Label final : Stringable<Label>, Hashable<Label>, Identifiable
    {
    private:
        std::string_view _name;

    public:
        /// @brief Creates the null label.
        constexpr Label() noexcept = default;

        /// @brief Creates an unbound label.
        constexpr explicit Label(std::string_view name) noexcept :
            _name(name) {}

        /// @brief Creates a label bound to an identifier.
        constexpr Label(std::string_view name, Link<const Identifier> uid) noexcept :
            Identifiable(uid),
            _name(name) {}

        /// @brief Gets the name.
        [[nodiscard]] constexpr std::string_view GetName() const noexcept { return _name; }

        /// @brief Checks that the label has a name.
        [[nodiscard]] constexpr bool IsValid() const noexcept { return !_name.empty(); }

        /// @brief Checks that the label is bound to an identifier.
        [[nodiscard]] constexpr bool IsBound() const noexcept { return !_uid.IsNull(); }

        /// @brief Checks that the label is bound to a certain identifier.
        [[nodiscard]] constexpr bool IsBound(Link<const Identifier> uid) const noexcept { return _uid == uid; }

        /// @brief Creates a label bound to a new identifier.
        [[nodiscard]] static Label CreateBound(std::string_view name);

        /// @brief Gets the text form.
        [[nodiscard]] std::string ToString() const;

        [[nodiscard]] constexpr bool operator==(Link<const Label> other) const noexcept
        {
            return _uid == other._uid && _name == other._name;
        }
    };

    /// @brief The null label.
    inline constexpr Label NullLabel{};
}
