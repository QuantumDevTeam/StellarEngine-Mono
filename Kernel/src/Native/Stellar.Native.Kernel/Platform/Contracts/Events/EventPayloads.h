// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Events/EventPayloads.h
/// @brief Typed payload alternatives carried by translated platform events.
///
/// EventPayload is a closed std::variant. Each EventType maps to one of the
/// alternatives below; events without extra data use std::monostate. The
/// payload objects are value types, but some fields are non-owning pointers to
/// backend data that is valid only for the duration of event dispatch.
#pragma once
// ReSharper disable CppClangTidyClangDiagnosticPadded

#include <string>
#include <variant>

#include "Platform/Types.h" // NOLINT
#include "Platform/Contracts/Core/Enums.h"

#include "Math/Vector/Vector2.h"
#include "Math/Shapes/Rect.h"

namespace Stellar::Native::Kernel::Platform::Events
{
    /// @brief Keyboard key transition data.
    struct API KeyPayload final
    {
        KeyCode Key = KeyCode::Unknown;      ///< Logical key that was pressed or released.
        uint32_t Scancode = 0;               ///< Backend scan code, or 0 when unavailable.
        ModifierKeys Mods = ModifierKeys::None; ///< Modifier and mouse-button state at the time of the event.
    };

    /// @brief Unicode text or dead-key data.
    struct API TextInputPayload final
    {
        char32_t Codepoint = 0;   ///< Unicode scalar value produced by the key.
        uint16_t RepeatCount = 1; ///< Number of repeated characters (usually 1).
        bool IsDeadKey = false;   ///< True when the codepoint is a dead-key marker, not final text.
    };

    // Mouse

    /// @brief Mouse-button transition data.
    struct API MouseButtonPayload final
    {
        MouseButton Button = MouseButton::None; ///< Button that changed state.
        ModifierKeys Mods = ModifierKeys::None; ///< Modifier and mouse-button state after the change.
        Math::Vectors::Vector2<int32_t> Pos{};  ///< Cursor position in client coordinates.
    };

    /// @brief Wheel rotation data.
    struct API MouseScrollPayload final
    {
        float DeltaX = 0.0f; ///< Horizontal wheel delta; positive scrolls right.
        float DeltaY = 0.0f; ///< Vertical wheel delta; positive scrolls up/away from the user.
        ModifierKeys Mods = ModifierKeys::None; ///< Modifier and mouse-button state during the scroll.
        Math::Vectors::Vector2<int32_t> Pos{};  ///< Cursor position in client coordinates.
    };

    /// @brief Cursor movement or hover data.
    struct API MouseMovePayload final
    {
        Math::Vectors::Vector2<int32_t> Pos{}; ///< Absolute cursor position in client coordinates.
        Math::Vectors::Vector2<float> Rel{};   ///< Relative motion since the previous event, when provided by the backend.
        ModifierKeys Mods = ModifierKeys::None;///< Modifier and mouse-button state during the movement.
    };

    // Window

    /// @brief Generic window geometry data.
    struct API WindowPayload final
    {
        Math::Vectors::Vector2<int32_t> Pos{};  ///< Window position in native screen coordinates.
        Math::Vectors::Vector2<uint32_t> Size{};///< Window size in pixels; zero when not applicable.
    };

    /// @brief Window minimized/maximized/restored transition.
    struct API WindowStatePayload final
    {
        /// @brief New window state reported by the backend.
        enum class API State : uint8_t
        {
            Normal,    ///< Normal restored window.
            Minimized, ///< Window is minimized.
            Maximized, ///< Window is maximized.
            Restored,  ///< Window was just restored from minimized/maximized.
        };

        State Kind = State::Normal; ///< State transition that produced the event.
    };

    /// @brief Window focus and activation details.
    struct API ActivatePayload final
    {
        /// @brief Kind of activation transition.
        enum class API Action : uint8_t
        {
            Activate,      ///< Window gained focus.
            ClickActivate, ///< Window gained focus because the user clicked it.
            Deactivate,    ///< Window lost focus.
            ActivateApp,   ///< Application gained foreground activation.
            DeactivateApp, ///< Application lost foreground activation.
        };

        Action Kind = Action::Activate; ///< Activation transition that occurred.
        bool Minimized = false;         ///< True when the window is minimized during the transition.
        uint32_t ThreadId = 0;          ///< Native thread identifier associated with the transition, or 0.
        Ptr<const Window::IWindow> Other = nullptr; ///< Other window involved in the transition, when known.
    };

    /// @brief DPI change notification.
    struct API DpiChangedPayload final
    {
        uint32_t DpiX = 0;                     ///< New horizontal dots-per-inch value.
        uint32_t DpiY = 0;                     ///< New vertical dots-per-inch value.
        Math::Shapes::Rect<int32_t> Suggested{};///< Rectangle suggested by the OS for the resized window.
    };

    // System notifications

    /// @brief Display topology or mode change.
    struct API DisplayChangePayload final
    {
        uintptr_t Monitor = 0; ///< Native monitor handle, or 0 when not specific to one display.
    };

    /// @brief Power-management notification.
    struct API PowerPayload final
    {
        uint32_t Event = 0; ///< Native power event code (suspend, resume, battery, ...).
    };

    // Drag & drop

    /// @brief Drag-and-drop notification.
    struct API DropPayload final
    {
        int32_t Count = 0;             ///< Number of dropped items, or 0 when not applicable.
        Ptr<const std::string> Paths = nullptr; ///< Backend-owned path data; valid only during dispatch.
    };

    // Gamepad

    /// @brief Gamepad button or connection notification.
    struct API GamepadButtonPayload final
    {
        uint32_t GamepadId = 0;                  ///< Backend-assigned controller index.
        GamepadButton Button = GamepadButton::None; ///< Button that changed state, if any.
    };

    /// @brief Gamepad analog-axis notification.
    struct API GamepadAxisPayload final
    {
        uint32_t GamepadId = 0;              ///< Backend-assigned controller index.
        GamepadAxis Axis = GamepadAxis::LeftX; ///< Axis that changed.
        float Value = 0.0f;                  ///< New axis value in the axis's documented range.
    };

    // Unknown

    /// @brief Fallback payload for events the translator does not model.
    struct API UnknownPayload final
    {
        uint32_t Msg = 0;     ///< Native message/event code.
        uintptr_t wParam{};   ///< First native parameter.
        uintptr_t lParam{};   ///< Second native parameter.
    };

    // platformed

    /// @brief Windows-specific payload structures.
    ///
    /// The names and fields mirror Windows messages, but native handles stay
    /// opaque so the kernel contract does not include windows.h.
    namespace WinAPI
    {
        // window

        /// @brief Non-client hit-test result.
        struct API NcHitTestPayload final
        {
            Math::Vectors::Vector2<int16_t> Pos{}; ///< Cursor position in native screen coordinates.
        };

        /// @brief Non-client size calculation input.
        struct API NcCalcSizePayload final
        {
            bool HasValidRects = false;             ///< True when the payload describes valid client rectangles.
            Math::Vectors::Vector2<int32_t> Pos{};  ///< Proposed client-area position.
            Math::Vectors::Vector2<uint32_t> Size{};///< Proposed client-area size.
        };

        /// @brief Cursor selection request.
        struct API CursorPayload final
        {
            uint16_t HitCode = 0; ///< Native hit-test code describing the cursor location.
            uintptr_t Handle = 0; ///< Native cursor handle, or 0 when the backend chooses the cursor.
        };

        /// @brief Device-change notification.
        struct API DeviceChangePayload final
        {
            uint32_t Event = 0;         ///< Native device-change event code.
            Ptr<const void> Data = nullptr; ///< Event-specific data; valid only during dispatch.
        };

        /// @brief Window show/hide notification.
        struct API ShowWindowPayload final
        {
            bool Shown = false; ///< True when the window became visible.
        };

        /// @brief Window enable/disable notification.
        struct API EnablePayload final
        {
            bool Enabled = false; ///< True when the window became enabled.
        };

        // Command / menu

        /// @brief Menu, accelerator or control command.
        struct API CommandPayload final
        {
            /// @brief Source of a command notification.
            enum class API Origin : std::uint8_t
            {
                Menu        = 0,       ///< Command originated from a menu item.
                Accelerator = 1,///< Command originated from a keyboard accelerator.
                Control     = 2,    ///< Command originated from a child control.
            };

            uint16_t Id = 0;       ///< Command identifier.
            uint16_t Notify = 0;   ///< Notification code for control commands, or 0.
            Origin From = Origin::Menu; ///< Where the command came from.
            uintptr_t Control = 0; ///< Native control handle for control commands, or 0.
        };

        /// @brief System-menu command.
        struct API SysCommandPayload final
        {
            uint32_t Command = 0; ///< System command code with the low reserved bits masked.
            int16_t X = 0;        ///< Cursor X coordinate in native screen coordinates.
            int16_t Y = 0;        ///< Cursor Y coordinate in native screen coordinates.
        };

        /// @brief Menu selection change.
        struct API MenuSelectPayload final
        {
            uint16_t Item = 0;   ///< Selected item identifier, or 0xFFFF for no item.
            uint16_t Flags = 0;  ///< Native menu-selection flags.
            uintptr_t Menu = 0;  ///< Native menu handle, or 0.
        };

        /// @brief Menu initialization notification.
        struct API InitMenuPayload final
        {
            uintptr_t Menu = 0; ///< Native menu handle about to be displayed.
        };

        // Timer

        /// @brief Timer expiration notification.
        struct API TimerPayload final
        {
            uintptr_t Timer = 0; ///< Native timer identifier that expired.
        };

        // Scroll

        /// @brief Horizontal or vertical scroll request.
        struct API ScrollPayload final
        {
            uint16_t Request = 0; ///< Native scroll request code (line, page, thumb, ...).
            int16_t ThumbPos = 0; ///< Thumb position for thumb-track requests, or 0.
            uintptr_t Control = 0;///< Native control handle that generated the scroll, or 0.
        };

        // System notifications

        /// @brief Session change notification.
        struct API SessionChangePayload final
        {
            uint32_t Event = 0; ///< Native session-change event code.
        };

        /// @brief System setting change notification.
        struct API SettingChangePayload final
        {
            uint32_t Flags = 0;                 ///< Native setting-change flags.
            Ptr<const wchar_t> Section = nullptr; ///< Changed section name; valid only during dispatch.
        };
    }

    // Variant

    /// @brief Type-erased payload stored in Event.
    ///
    /// std::monostate means the event carries no additional data. Use
    /// Event::TryGet<P>() for an optional lookup or Event::Get<P>() when the
    /// payload type is known from Event::Type.
    using EventPayload = std::variant<
        std::monostate,
        KeyPayload,
        TextInputPayload,
        MouseButtonPayload,
        MouseScrollPayload,
        MouseMovePayload,
        WindowPayload,
        WindowStatePayload,
        ActivatePayload,
        DpiChangedPayload,
        DisplayChangePayload,
        PowerPayload,
        DropPayload,
        GamepadButtonPayload,
        GamepadAxisPayload,
        UnknownPayload,
        // Windows
        WinAPI::NcHitTestPayload,
        WinAPI::NcCalcSizePayload,
        WinAPI::CursorPayload,
        WinAPI::DeviceChangePayload,
        WinAPI::ShowWindowPayload,
        WinAPI::EnablePayload,
        WinAPI::CommandPayload,
        WinAPI::SysCommandPayload,
        WinAPI::MenuSelectPayload,
        WinAPI::InitMenuPayload,
        WinAPI::TimerPayload,
        WinAPI::ScrollPayload,
        WinAPI::SessionChangePayload,
        WinAPI::SettingChangePayload
    >;
}
