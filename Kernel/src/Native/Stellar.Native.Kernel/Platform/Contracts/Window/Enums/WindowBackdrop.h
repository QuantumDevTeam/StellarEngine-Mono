// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Window/Enums/WindowBackdrop.h
/// @brief Window backdrop material selection.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform::Window
{
    /// @brief System backdrop material drawn behind a window.
    ///
    /// Materials are backend- and OS-dependent. A backend that cannot render
    /// the requested material falls back to None; callers should treat the
    /// value as a hint, not as a guarantee.
    enum class API WindowBackdrop : uint8_t
    {
        Auto = 0, ///< Let the backend choose a material appropriate for the current OS/theme.
        None,     ///< No system backdrop; the window paints its own background.
        Mica,     ///< Windows 11 Mica material derived from the desktop wallpaper.
        MicaAlt,  ///< Mica variant with stronger contrast/tinting.
        Acrylic,  ///< Acrylic blur material.
        Tabbed,   ///< Tabbed/backdrop variant used by tabbed window shells.
    };

    /// @brief Returns the name of a backdrop material.
    /// @param e Backdrop value to describe.
    /// @return Static English name, or "unknown" for values outside the enum.
    inline const char* to_string(WindowBackdrop e)
    {
        switch (e)
        {
        case WindowBackdrop::Auto: return "Auto";
        case WindowBackdrop::None: return "None";
        case WindowBackdrop::Mica: return "Mica";
        case WindowBackdrop::MicaAlt: return "MicaAlt";
        case WindowBackdrop::Acrylic: return "Acrylic";
        case WindowBackdrop::Tabbed: return "Tabbed";
        default: return "unknown";
        }
    }
}
