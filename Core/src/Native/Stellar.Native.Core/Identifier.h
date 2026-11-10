// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Identifier.h
/// @brief The unique identifier of the engine.
#pragma once

#include <algorithm>
#include <array>
#include <compare>
#include <cstdint>
#include <optional>
#include <string>
#include <string_view>

#include "Mixins/Hashable.h"

namespace Stellar::Native::Core
{
    /// @brief A 128-bit unique identifier.
    ///
    /// This is the base of the whole engine: native systems address their objects by identifier, so managed code
    /// passes an identifier instead of marshalling the object. It is a plain value of exactly 16 bytes: the memory
    /// layout of StellarUid, of a Windows GUID and of `System.Guid`, so it crosses the managed boundary as it is.
    ///
    /// It implements the hashing and text contracts itself instead of taking the mixins: they are built on top of
    /// an identifier.
    struct STELLAR_CORE_API Identifier final
    {
        /// @brief Storage of an identifier.
        using Bytes = std::array<uint8_t, 16>;

    private:
        Bytes _data{};

    public:
        /// @brief Creates the null identifier.
        constexpr Identifier() noexcept = default;

        /// @brief Creates an identifier from its bytes.
        constexpr explicit Identifier(Link<const Bytes> bytes) noexcept :
            _data(bytes) {}

        /// @brief Creates an identifier from its ABI form.
        constexpr explicit Identifier(Link<const StellarUid> uid) noexcept
        {
            std::copy_n(uid.bytes, _data.size(), _data.begin());
        }

        /// @brief Gets the bytes.
        [[nodiscard]] constexpr Link<const Bytes> GetData() const noexcept { return _data; }

        /// @brief Gets the ABI form.
        [[nodiscard]] constexpr StellarUid ToAbi() const noexcept
        {
            StellarUid uid{};
            std::copy_n(_data.begin(), _data.size(), uid.bytes);
            return uid;
        }

        /// @brief Checks for the null identifier.
        [[nodiscard]] constexpr bool IsNull() const noexcept { return _data == Bytes{}; }

        /// @brief Gets the null identifier.
        [[nodiscard]] static constexpr Identifier Null() noexcept { return {}; }

        /// @brief Creates a new random identifier.
        /// @throws Failures::NativeException if the system cannot provide one.
        [[nodiscard]] static Identifier Create();

        /// @brief Parses the canonical form: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`, in any letter case.
        /// @return The identifier, or nothing if the text is malformed.
        [[nodiscard]] static std::optional<Identifier> Parse(std::string_view text) noexcept;

        /// @brief Gets the canonical form, the same one `System.Guid` prints.
        [[nodiscard]] std::string ToString() const;

        /// @brief Computes the hash code.
        [[nodiscard]] uint64_t GetHashCode() const noexcept;

        [[nodiscard]] constexpr bool operator==(Link<const Identifier> other) const noexcept { return _data == other._data; }

        [[nodiscard]] constexpr auto operator<=>(Link<const Identifier> other) const noexcept { return _data <=> other._data; }
    };

    static_assert(sizeof(Identifier) == sizeof(StellarUid), "Identifier must stay layout-compatible with StellarUid");
    static_assert(std::is_standard_layout_v<Identifier>, "Identifier must stay a plain value");
    static_assert(std::is_trivially_copyable_v<Identifier>, "Identifier must stay a plain value");

    /// @brief The null identifier.
    inline constexpr Identifier NullIdentifier = Identifier::Null();
}
