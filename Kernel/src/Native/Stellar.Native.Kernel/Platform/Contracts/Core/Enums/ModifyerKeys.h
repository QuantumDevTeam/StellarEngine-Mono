// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Core/Enums/ModifyerKeys.h
/// @brief Modifier-key and mouse-button bit mask.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform
{
    /// @brief Bit mask of modifier keys and mouse buttons held during an event.
    ///
    /// The value is attached to keyboard, mouse and text payloads so handlers
    /// can implement shortcuts without tracking key state separately. Lock
    /// flags describe toggle state, not physical key presses.
    enum class API ModifierKeys : uint32_t
    {
        None       = 0,           ///< No modifiers are active.
        Shift      = 1u << 0,     ///< Shift is held.
        Control    = 1u << 1,     ///< Control is held.
        Alt        = 1u << 2,     ///< Alt is held.
        Super      = 1u << 3,     ///< Super/Windows/Command is held.
        CapsLock   = 1u << 4,     ///< Caps Lock is active.
        NumLock    = 1u << 5,     ///< Num Lock is active.
        ScrollLock = 1u << 6,     ///< Scroll Lock is active.
        LeftBtn    = 1u << 7,     ///< Left mouse button is held.
        RightBtn   = 1u << 8,     ///< Right mouse button is held.
        MiddleBtn  = 1u << 9,     ///< Middle mouse button is held.
    };
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    /// @brief Enables bitwise flag operators for Platform::ModifierKeys.
    template <> struct IsFlagEnum<ModifierKeys> : std::true_type {};
}
