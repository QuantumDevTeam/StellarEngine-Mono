// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Events/EventTypes.h
/// @brief Catalog of translated kernel event types.
///
/// EventType identifies the semantic kind of an Event and therefore the
/// payload alternative the event carries. Numeric ranges keep related events
/// together for debugging; they are not a native ABI and backends should map
/// their own message constants to these values.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform::Events
{
    /// @brief Semantic kind of a translated platform event.
    ///
    /// The enumerator documents which EventPayload alternative is normally
    /// stored in Event::Payload. Some notifications intentionally use
    /// std::monostate when they carry no additional data.
    enum class API EventType : uint32_t // NOLINT(readability-enum-initial-value)
    {
        Unknown = 0, ///< Unrecognized event; payload is usually UnknownPayload or monostate.

        // Keyboard
        KeyDown = 1,    ///< A key was pressed; payload is KeyPayload. Check EventFlags::Repeat for auto-repeat.
        KeyUp,          ///< A key was released; payload is KeyPayload.
        TextInput,      ///< A text codepoint was produced; payload is TextInputPayload.
        TextDeadChar,   ///< A dead key was pressed; payload is TextInputPayload with IsDeadKey set.

        // Mouse
        MouseMove = 100,      ///< Cursor moved over the window; payload is MouseMovePayload.
        MouseButtonDown,      ///< A mouse button was pressed; payload is MouseButtonPayload.
        MouseButtonUp,        ///< A mouse button was released; payload is MouseButtonPayload.
        MouseScroll,          ///< The wheel was rotated; payload is MouseScrollPayload.
        MouseEnter,           ///< Cursor entered the window; payload is MouseMovePayload.
        MouseLeave,           ///< Cursor left the window; payload is typically std::monostate.
        MouseHover,           ///< Cursor hovered without moving; payload is MouseMovePayload.

        // Window
        WindowCreate = 200,   ///< Native window was created; payload is WindowPayload.
        WindowClose,          ///< Close was requested; payload is WindowPayload. Handlers may consume it.
        WindowDestroy,        ///< Native window is being destroyed; payload is WindowPayload.
        WindowNcDestroy,      ///< Non-client area is being destroyed; payload is WindowPayload.
        WindowFocus,          ///< Window gained keyboard focus; payload is ActivatePayload.
        WindowBlur,           ///< Window lost keyboard focus; payload is ActivatePayload.
        WindowActivate,       ///< Window became active; payload is ActivatePayload.
        WindowDeactivate,     ///< Window became inactive; payload is ActivatePayload.
        WindowActivateApp,    ///< Application became active; payload is ActivatePayload.
        WindowDeactivateApp,  ///< Application became inactive; payload is ActivatePayload.
        WindowResize,         ///< Window client size changed; payload is WindowPayload.
        WindowMove,           ///< Window position changed; payload is WindowPayload.
        WindowMinimize,       ///< Window was minimized; payload is WindowStatePayload.
        WindowMaximize,       ///< Window was maximized; payload is WindowStatePayload.
        WindowRestore,        ///< Window was restored from minimized/maximized; payload is WindowStatePayload.
        WindowShow,           ///< Window visibility changed; payload is WinAPI::ShowWindowPayload.
        WindowEnable,         ///< Window enabled state changed; payload is WinAPI::EnablePayload.
        WindowDisplayChange, ///< Window moved to another display; payload is DisplayChangePayload.
        WindowDpiChange,      ///< Window DPI changed; payload is DpiChangedPayload.
        WindowNcHitTest,      ///< Non-client hit test; payload is WinAPI::NcHitTestPayload.
        WindowNcCalcSize,     ///< Non-client size calculation; payload is WinAPI::NcCalcSizePayload.
        WindowGetMinMaxInfo,  ///< Min/max tracking size query; payload is backend-specific.
        WindowPaint,          ///< Window needs repainting; payload is typically std::monostate.
        WindowEraseBackground,///< Window background needs erasing; payload is typically std::monostate.
        WindowSetCursor,      ///< Cursor must be set; payload is WinAPI::CursorPayload.
        WindowCaptureChanged, ///< Mouse capture changed; payload is WindowPayload.
        WindowDeviceChange,   ///< Device change notification; payload is WinAPI::DeviceChangePayload.

        // Command / menu
        Command = 260,  ///< Menu, accelerator or control command; payload is WinAPI::CommandPayload.
        SysCommand,     ///< System-menu command; payload is WinAPI::SysCommandPayload.
        MenuSelect,     ///< Menu item selection changed; payload is WinAPI::MenuSelectPayload.
        InitMenu,       ///< Menu is about to be displayed; payload is WinAPI::InitMenuPayload.
        InitMenuPopup,  ///< Popup menu is about to be displayed; payload is WinAPI::InitMenuPayload.

        // Timer
        Timer = 280,    ///< Timer expired; payload is WinAPI::TimerPayload.

        // Scroll
        ScrollH = 290,  ///< Horizontal scroll; payload is WinAPI::ScrollPayload.
        ScrollV,        ///< Vertical scroll; payload is WinAPI::ScrollPayload.

        // System notifications
        DisplayChange = 400, ///< Display mode/topology changed; payload is DisplayChangePayload.
        ClipboardUpdate,     ///< Clipboard contents changed; payload is typically std::monostate.
        PowerBroadcast,      ///< Power-management notification; payload is PowerPayload.
        SessionChange,       ///< Terminal-services session change; payload is WinAPI::SessionChangePayload.
        SettingChange,       ///< System setting changed; payload is WinAPI::SettingChangePayload.
        ThemeChanged,        ///< System theme changed; payload is typically std::monostate.
        TimeChange,          ///< System time changed; payload is typically std::monostate.

        // Drag & drop
        Drop = 250,     ///< Files were dropped; payload is DropPayload.
        DropEnter,      ///< Drag operation entered the window; payload is DropPayload.
        DropLeave,      ///< Drag operation left the window; payload is typically std::monostate.

        // Gamepad
        JoyDeviceConnect = 300, ///< Gamepad connected; payload is GamepadButtonPayload or backend-specific.
        JoyDeviceDisconnect,    ///< Gamepad disconnected; payload is GamepadButtonPayload or backend-specific.
        JoyAxisMotion,          ///< Gamepad axis moved; payload is GamepadAxisPayload.
        JoyButtonDown,          ///< Gamepad button pressed; payload is GamepadButtonPayload.
        JoyButtonUp,            ///< Gamepad button released; payload is GamepadButtonPayload.
        JoyHatMotion,           ///< Gamepad hat (D-pad) changed; payload is backend-specific.

        // Application
        Quit = 0xFFFFFFFFu, ///< Application quit request; payload is typically std::monostate.
    };

    /// @brief Returns the name of an event type.
    /// @param e Event type to describe.
    /// @return Static English name, or "unknown" for values not in the catalog.
    inline const char* to_string(EventType e)
    {
        switch (e)
        {
        case EventType::Unknown: return "Unknown";
        case EventType::KeyDown: return "KeyDown";
        case EventType::KeyUp: return "KeyUp";
        case EventType::TextInput: return "TextInput";
        case EventType::TextDeadChar: return "TextDeadChar";
        case EventType::MouseMove: return "MouseMove";
        case EventType::MouseButtonDown: return "MouseButtonDown";
        case EventType::MouseButtonUp: return "MouseButtonUp";
        case EventType::MouseScroll: return "MouseScroll";
        case EventType::MouseEnter: return "MouseEnter";
        case EventType::MouseLeave: return "MouseLeave";
        case EventType::MouseHover: return "MouseHover";
        case EventType::WindowCreate: return "WindowCreate";
        case EventType::WindowClose: return "WindowClose";
        case EventType::WindowDestroy: return "WindowDestroy";
        case EventType::WindowNcDestroy: return "WindowNcDestroy";
        case EventType::WindowFocus: return "WindowFocus";
        case EventType::WindowBlur: return "WindowBlur";
        case EventType::WindowActivate: return "WindowActivate";
        case EventType::WindowDeactivate: return "WindowDeactivate";
        case EventType::WindowActivateApp: return "WindowActivateApp";
        case EventType::WindowDeactivateApp: return "WindowDeactivateApp";
        case EventType::WindowResize: return "WindowResize";
        case EventType::WindowMove: return "WindowMove";
        case EventType::WindowMinimize: return "WindowMinimize";
        case EventType::WindowMaximize: return "WindowMaximize";
        case EventType::WindowRestore: return "WindowRestore";
        case EventType::WindowShow: return "WindowShow";
        case EventType::WindowEnable: return "WindowEnable";
        case EventType::WindowDisplayChange: return "WindowDisplayChange";
        case EventType::WindowDpiChange: return "WindowDpiChange";
        case EventType::WindowNcHitTest: return "WindowNcHitTest";
        case EventType::WindowNcCalcSize: return "WindowNcCalcSize";
        case EventType::WindowGetMinMaxInfo: return "WindowGetMinMaxInfo";
        case EventType::WindowPaint: return "WindowPaint";
        case EventType::WindowEraseBackground: return "WindowEraseBackground";
        case EventType::WindowSetCursor: return "WindowSetCursor";
        case EventType::WindowCaptureChanged: return "WindowCaptureChanged";
        case EventType::WindowDeviceChange: return "WindowDeviceChange";
        case EventType::Command: return "Command";
        case EventType::SysCommand: return "SysCommand";
        case EventType::MenuSelect: return "MenuSelect";
        case EventType::InitMenu: return "InitMenu";
        case EventType::InitMenuPopup: return "InitMenuPopup";
        case EventType::Timer: return "Timer";
        case EventType::ScrollH: return "ScrollH";
        case EventType::ScrollV: return "ScrollV";
        case EventType::DisplayChange: return "DisplayChange";
        case EventType::ClipboardUpdate: return "ClipboardUpdate";
        case EventType::PowerBroadcast: return "PowerBroadcast";
        case EventType::SessionChange: return "SessionChange";
        case EventType::SettingChange: return "SettingChange";
        case EventType::ThemeChanged: return "ThemeChanged";
        case EventType::TimeChange: return "TimeChange";
        case EventType::Drop: return "Drop";
        case EventType::DropEnter: return "DropEnter";
        case EventType::DropLeave: return "DropLeave";
        case EventType::JoyDeviceConnect: return "JoyDeviceConnect";
        case EventType::JoyDeviceDisconnect: return "JoyDeviceDisconnect";
        case EventType::JoyAxisMotion: return "JoyAxisMotion";
        case EventType::JoyButtonDown: return "JoyButtonDown";
        case EventType::JoyButtonUp: return "JoyButtonUp";
        case EventType::JoyHatMotion: return "JoyHatMotion";
        case EventType::Quit: return "Quit";
        default: return "unknown";
        }
    }
}
