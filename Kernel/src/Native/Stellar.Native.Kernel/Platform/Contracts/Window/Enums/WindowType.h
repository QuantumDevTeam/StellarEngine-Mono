// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Window/Enums/WindowType.h
/// @brief Native window kinds.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform::Window
{
    /// @brief Native window kind requested at creation.
    ///
    /// The type selects the native window class/style family. It is distinct
    /// from WindowState: a Popup or Overlapped window can later be minimized,
    /// maximized or made fullscreen.
    enum class API WindowType : uint8_t
    {
        None = 0,   ///< Unset/invalid type.
        Overlapped, ///< Standard top-level window with a frame and caption.
        Fullscreen, ///< Top-level window intended to cover the whole display.
        Popup,      ///< Frameless top-level window (menus, tooltips, splash screens).
        Child,      ///< Child window embedded in another window's client area.
        Toolbox,    ///< Tool window with a compact caption and no taskbar entry.
    };

    /// @brief Returns the name of a window type.
    /// @param e Type to describe.
    /// @return Static English name, or "unknown" for values outside the enum.
    inline const char* to_string(WindowType e)
    {
        switch (e)
        {
        case WindowType::None: return "None";
        case WindowType::Overlapped: return "Overlapped";
        case WindowType::Fullscreen: return "Fullscreen";
        case WindowType::Popup: return "Popup";
        case WindowType::Child: return "Child";
        case WindowType::Toolbox: return "Toolbox";
        default: return "unknown";
        }
    }
}
