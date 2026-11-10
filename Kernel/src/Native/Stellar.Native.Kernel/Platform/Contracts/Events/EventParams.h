// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Events/EventParams.h
/// @brief Raw backend-native event parameters passed to translators.
#pragma once

namespace Stellar::Native::Kernel::Platform::Events
{
    /// @brief Opaque native message parameters.
    ///
    /// EventParams is the smallest common denominator between native event
    /// loops: two pointer-sized words. Translators interpret them according to
    /// the native message type (for example wParam/lParam on Windows). The
    /// helpers below cover the common packed 16-bit encodings.
    struct EventParams final
    {
        uintptr_t wParam = 0; ///< First native parameter (message-specific meaning).
        uintptr_t lParam = 0; ///< Second native parameter (message-specific meaning).

        /// @brief Creates zero-initialized native parameters.
        constexpr EventParams() noexcept = default;

        /// @brief Creates parameters from raw native words.
        /// @param w First native parameter value.
        /// @param l Second native parameter value.
        constexpr EventParams(uintptr_t w, uintptr_t l) noexcept
            : wParam(w),
              lParam(l) {}

        // low/high 16-bit helpers

        /// @brief Low 16 bits of wParam.
        /// @return Low 16 bits as unsigned.
        [[nodiscard]] constexpr uint16_t lowW() const noexcept { return static_cast<uint16_t>(wParam & 0xFFFFu); }

        /// @brief High 16 bits of wParam.
        /// @return High 16 bits as unsigned.
        [[nodiscard]] constexpr uint16_t highW() const noexcept
        {
            return static_cast<uint16_t>(wParam >> 16 & 0xFFFFu);
        }

        /// @brief Low 16 bits of lParam.
        /// @return Low 16 bits as unsigned.
        [[nodiscard]] constexpr uint16_t lowL() const noexcept { return static_cast<uint16_t>(lParam & 0xFFFFu); }

        /// @brief High 16 bits of lParam.
        /// @return High 16 bits as unsigned.
        [[nodiscard]] constexpr uint16_t highL() const noexcept
        {
            return static_cast<uint16_t>((lParam >> 16) & 0xFFFFu);
        }

        // packed 16-bit signed coords

        /// @brief Signed X coordinate packed in the low 16 bits of lParam.
        /// @return X coordinate as signed 16-bit integer.
        [[nodiscard]] constexpr int16_t coordX() const noexcept { return static_cast<int16_t>(lParam & 0xFFFFu); }

        /// @brief Signed Y coordinate packed in the high 16 bits of lParam.
        /// @return Y coordinate as signed 16-bit integer.
        [[nodiscard]] constexpr int16_t coordY() const noexcept
        {
            return static_cast<int16_t>(lParam >> 16 & 0xFFFFu);
        }
    };
}
