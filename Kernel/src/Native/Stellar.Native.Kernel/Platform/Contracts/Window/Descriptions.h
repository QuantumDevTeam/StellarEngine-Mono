// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Window/Descriptions.h
/// @brief Creation parameters for native windows and window classes.
#pragma once

#include <string>

#include "Platform/Types.h" // NOLINT
#include "Enums.h"

#include "Graphic/Color4.h"
#include "Math/Shapes/Rect.h"

namespace Stellar::Native::Kernel::Platform::Window
{
    /// @brief Visual style applied to a window.
    ///
    /// The style is mutable and is referenced by WindowDesc/WindowClassDesc
    /// through non-owning pointers. Backends may treat fields as hints and
    /// ignore features the platform cannot provide.
    struct API WindowStyleDesc final
    {
        // style

        /// @brief Request the platform's dark title bar and system theme.
        bool IsDarkMode = false;

        /// @brief Request per-pixel alpha composition instead of a solid background.
        bool IsPixelTransparency = false;

        /// @brief Overall window opacity in the range [0, 255]; 255 is fully opaque.
        uint8_t Opacity = 255;

        /// @brief System backdrop material drawn behind the window.
        WindowBackdrop BackdropType = WindowBackdrop::None;

        /// @brief Border color; the default (-1 in every component) means "not specified".
        Graphics::Color4 BorderColor{-1};

        /// @brief Caption color; the default (-1 in every component) means "not specified".
        Graphics::Color4 CaptionColor{-1};

        // Ptr<Icon::IconDesc> Icon = nullptr; // TODO: ...
        // Ptr<Cursor::CursorDesc> Cursor = nullptr; // TODO: ...
    };

    /// @brief Parameters used to create one native window.
    ///
    /// The manager takes ownership of the WindowDesc passed to MakeWindow().
    /// Pointers inside the description are non-owning and must stay valid for
    /// as long as the window uses them.
    struct API WindowDesc final
    {
        /// @brief Window title displayed in the caption and taskbar.
        std::string Title{};

        /// @brief Initial window rectangle in native screen coordinates.
        Math::Shapes::Rect<int32_t> Rect{};

        /// @brief Native window kind (overlapped, popup, child, ...).
        WindowType Type = WindowType::None;

        /// @brief Behavioral flags such as resizable, borderless or always-on-top.
        WindowFlags Flags = WindowFlags::None;

        // Non-owning; the caller keeps the style alive.
        /// @brief Optional visual style; null selects the class/default style.
        Ptr<WindowStyleDesc> Style = nullptr;

        /// @brief Optional parent window for child/owned windows; null for a top-level window.
        Ptr<const IWindow> Parent = nullptr;

        /// @brief Manager that owns the window; MakeWindow() fills this with itself.
        Ptr<IWindowManager> WindowManager = nullptr;

        /// @brief Registered window class that supplies the native window procedure.
        /// @note MakeWindow() requires a valid class; it is not optional.
        Ptr<const IWindowClass> WindowClass = nullptr;

        // bool IsFullScreen = false;
        // Ptr<Display::DisplayDesc> Display = nullptr; // TODO: ...
        // Ptr<Mouse::IWindowMouseProc> MouseProc = nullptr; // TODO: ...
    };

    /// @brief Parameters used to register a window class.
    ///
    /// A class groups windows that share a native window procedure, base type,
    /// base flags and default style. The manager takes ownership of the
    /// description passed to RegisterWindowClass().
    struct API WindowClassDesc final
    {
        /// @brief Unique class name used to identify and create windows.
        std::string Name{};

        /// @brief Event procedure that receives native messages for the class.
        Ptr<const Events::IEventProc> Proc = nullptr;

        /// @brief Base window type for windows of this class.
        WindowType Type = WindowType::None;

        /// @brief Base behavioral flags for windows of this class.
        WindowFlags Flags = WindowFlags::None;

        // Non-owning; the caller keeps the style and proc alive.
        /// @brief Default visual style applied to windows of this class.
        Ptr<WindowStyleDesc> BaseStyle = nullptr;
    };
}
