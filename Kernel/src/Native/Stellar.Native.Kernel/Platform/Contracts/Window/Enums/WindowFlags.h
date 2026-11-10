// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Window/Enums/WindowFlags.h
/// @brief Behavioral and capability flags for native windows.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform::Window
{
    /// @brief Bit flags that modify how a native window behaves.
    ///
    /// Flags are combined with `|` and passed through WindowDesc or
    /// WindowClassDesc. Unsupported flags are ignored or rejected by the
    /// backend; capabilities such as transparency and focus behavior are
    /// platform-specific.
    enum class API WindowFlags : uint32_t
    {
        None         = 0,        ///< No special behavior.
        Resizable    = 1u << 0,  ///< Allow the user to resize the window.
        Borderless   = 1u << 1,  ///< Hide the native frame and caption.
        AlwaysOnTop  = 1u << 2,  ///< Keep the window above normal windows.
        Transparent  = 1u << 3,  ///< Request transparent composition; exact behavior is backend-specific.
        Layered      = 1u << 4,  ///< Use a layered/composited native window where supported.
        AcceptsInput = 1u << 5,  ///< Window can receive pointer and keyboard input.
        AcceptsFocus = 1u << 6,  ///< Window can take keyboard focus.
        AppWindow    = 1u << 7,  ///< Use app-window styling/behavior where supported.
    };

    /// @brief Returns the name of a single window flag.
    /// @param e Exact flag value to describe.
    /// @return Static flag name, or "unknown" for combined/unsupported masks.
    inline const char* to_string(WindowFlags e)
    {
        switch (e)
        {
        case WindowFlags::None: return "None";
        case WindowFlags::Resizable: return "Resizable";
        case WindowFlags::Borderless: return "Borderless";
        case WindowFlags::AlwaysOnTop: return "AlwaysOnTop";
        case WindowFlags::Transparent: return "Transparent";
        case WindowFlags::Layered: return "Layered";
        case WindowFlags::AcceptsInput: return "AcceptsInput";
        case WindowFlags::AcceptsFocus: return "AcceptsFocus";
        case WindowFlags::AppWindow: return "AppWindow";
        default: return "unknown";
        }
    }
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    template <> struct IsFlagEnum<Window::WindowFlags> : std::true_type {};
}
