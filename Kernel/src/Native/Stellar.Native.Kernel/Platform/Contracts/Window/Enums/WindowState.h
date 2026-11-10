// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Window/Enums/WindowState.h
/// @brief High-level display state of a window.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform::Window
{
    /// @brief Current display state of a window.
    ///
    /// The state describes how the window is presented, not which flags were
    /// requested at creation. Query IWindow's IsMinimized()/IsMaximized()
    /// helpers for the common predicates.
    enum class API WindowState : uint8_t
    {
        Normal,    ///< Restored, non-fullscreen window.
        Minimized, ///< Window is minimized and not visible as a normal window.
        Maximized, ///< Window fills the work area but keeps its frame.
        Fullscreen,///< Window owns the full display, typically without a frame.
    };

    /// @brief Returns the name of a window state.
    /// @param e State to describe.
    /// @return Static English name, or "unknown" for values outside the enum.
    inline const char* to_string(WindowState e)
    {
        switch (e)
        {
        case WindowState::Normal: return "Normal";
        case WindowState::Minimized: return "Minimized";
        case WindowState::Maximized: return "Maximized";
        case WindowState::Fullscreen: return "Fullscreen";
        default: return "unknown";
        }
    }
}
