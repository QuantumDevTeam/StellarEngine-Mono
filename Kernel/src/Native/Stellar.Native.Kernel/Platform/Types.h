// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Types.h
/// @brief Forward declarations for the public platform contract surface.
///
/// The header is deliberately definition-free: it lets platform interfaces
/// refer to each other (events to windows, windows to events) without pulling
/// in the complete contract headers. Include the concrete header or the
/// Kernel/platform_contracts.h umbrella when definitions are required.
#pragma once

#include "../include/Kernel/macro.h"

/// @brief Platform contracts: windows, events, shell integration and backend services.
namespace Stellar::Native::Kernel::Platform
{
    /// @brief Event routing, translation, dispatch and typed event payloads.
    namespace Events
    {
        /// @brief Stores subscriptions and dispatches events to matching handlers.
        ///
        /// A manager is a mutable service: handlers can be added and removed at
        /// runtime, while Process() is logically const with respect to the
        /// subscription list.
        class API IEventManager;

        /// @brief Native event entry point.
        ///
        /// Receives backend-native messages, asks the translator to build an
        /// Event and forwards it to the registered event managers in order.
        class API IEventProc;

        /// @brief Converts backend-native messages into kernel Event values.
        ///
        /// Translators are backend-specific and stateless with respect to the
        /// event stream; one translator can serve every window of a backend.
        class API IEventTranslator;

        // exists

        /// @brief Raw native parameters (wParam/lParam) passed to translators.
        struct EventParams;

        /// @brief Translated event: type, flags, source window and typed payload.
        struct API Event;

        /// @brief Per-dispatch context: sender, frame/time and recursion depth.
        struct API EventContext;

        /// @brief Type-erased callable used for event subscriptions.
        ///
        /// The delegate stores a function pointer and an optional instance
        /// pointer; it does not own the instance.
        class API EventDelegate;

        /// @brief Per-event flags such as repeat, synthetic and handled.
        enum class API EventFlags : uint32_t;

        /// @brief Identifies the payload stored in Event.
        enum class API EventType : uint32_t;
    }

    /// @brief Native window creation, state and management.
    namespace Window
    {
        /// @brief One native window: state queries, setters and lifecycle.
        class API IWindow;

        /// @brief A registered native window class shared by windows of the same kind.
        ///
        /// The class fixes the native window procedure, base type, base flags
        /// and default visual style used when a window is created.
        class API IWindowClass;

        /// @brief Owns window classes and windows and pumps their native messages.
        class API IWindowManager;

        // exists

        /// @brief Visual style: dark mode, transparency, backdrop material and colors.
        struct API WindowStyleDesc;

        /// @brief Window creation parameters: title, rect, type, flags and parents.
        struct API WindowDesc;

        /// @brief Window class registration parameters: name, type, flags, style and proc.
        struct API WindowClassDesc;

        /// @brief System backdrop material behind the window.
        enum class API WindowBackdrop : uint8_t;

        /// @brief Bit flags controlling window behavior and capabilities.
        enum class API WindowFlags : uint32_t;

        /// @brief Current window state (normal, minimized, maximized, fullscreen).
        enum class API WindowState : uint8_t;

        /// @brief Native window kind (overlapped, popup, child, ...).
        enum class API WindowType : uint8_t;
    }

    /// @brief Windows Shell integration interfaces (thumbnail, preview, commands).
    ///
    /// The declarations use opaque `void*`/integer handles so the kernel does
    /// not depend on Windows Shell headers. Backend implementations are free to
    /// reinterpret the handles according to their native API.
    namespace Shell
    {
        /// @brief Provides file thumbnails to Explorer.
        class API IThumbnailProvider;

        /// @brief Hosts a file preview inside an Explorer preview pane.
        class API IPreviewHandler;

        /// @brief Explorer context-menu command.
        class API IExplorerCommand;

        /// @brief COM initialization from a file path.
        class API IInitializeWithFile;

        /// @brief COM initialization from a stream.
        class API IInitializeWithStream;

        /// @brief COM initialization from a shell item.
        class API IInitializeWithItem;
    }

    /// @brief Platform backend service: owns the default window and event managers.
    ///
    /// Obtained through Platform::MakePlatform<\B>(). The service is process-wide
    /// and non-owning from the caller's perspective: the backend keeps it alive
    /// until process exit.
    class API IPlatform;

    // TODO: IMPLEMENT
    /// @brief Icon resources attached to windows.
    ///
    /// @note The contract is reserved for future use; no icon factory is
    /// implemented yet.
    namespace Icon
    {
        /// @brief Icon description passed to windows (not implemented yet).
        // struct API IconDesc;
    }

    // TODO: IMPLEMENT
    /// @brief Mouse cursor resources attached to windows.
    ///
    /// @note The contract is reserved for future use; no cursor factory is
    /// implemented yet.
    namespace Cursor
    {
        /// @brief Cursor description passed to windows (not implemented yet).
        // struct API CursorDesc;
    }

    // TODO: IMPLEMENT
    /// @brief Display enumeration and mode management.
    ///
    /// @note The contract is reserved for future use; display enumeration and
    /// mode switching are not implemented yet.
    namespace Display
    {
        /// @brief Display mode and topology description (not implemented yet).
        // struct API DisplayDesc;

        /// @brief Enumerates displays and changes their modes (not implemented yet).
        // struct API IDisplayManager;
    }

    // TODO: IMPLEMENT
    /// @brief Direct mouse device access and per-window mouse hooks.
    ///
    /// @note The contract is reserved for future use; direct mouse devices and
    /// raw mouse input are not implemented yet.
    namespace Mouse
    {
        /// @brief Mouse device description (not implemented yet).
        //struct API IMouseDesc;

        /// @brief Mouse device state and capture (not implemented yet).
        //struct API IMouse;

        /// @brief Per-window mouse event hook (not implemented yet).
        //struct API IWindowMouseProc;
    }

    // exists

    /// @brief Gamepad analog axis identifier.
    enum class API GamepadAxis : uint8_t;

    /// @brief Gamepad button identifier.
    enum class API GamepadButton : uint8_t;

    /// @brief Keyboard key code.
    enum class API KeyCode : uint16_t;

    /// @brief Bit mask of modifier keys (Ctrl, Shift, Alt, ...).
    enum class API ModifierKeys : uint32_t;

    /// @brief Mouse button identifier.
    enum class API MouseButton : uint8_t;
}
