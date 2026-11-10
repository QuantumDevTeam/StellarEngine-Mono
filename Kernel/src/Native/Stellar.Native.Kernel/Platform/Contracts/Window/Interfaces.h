// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Window/Interfaces.h
/// @brief Native window, window-class and window-manager interfaces.
#pragma once

#include <span>
#include <string>
#include <string_view>

#include "Platform/Types.h" // NOLINT

#include "Math/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Platform::Window
{
    /// @brief One native window with state queries, setters and lifecycle.
    ///
    /// Windows are created through IWindowManager::MakeWindow() and owned by
    /// that manager. The interface does not expose ownership: callers receive
    /// non-owning pointers and must not delete windows directly. The native
    /// handle stays valid until Destroy() or manager-driven destruction.
    class API IWindow // NOLINT
    {
    public:
        virtual ~IWindow() = default;

        // native access

        /// @brief Returns the backend-native window handle.
        /// @return Opaque native handle (HWND, xdg_toplevel*, ...), or null after destruction.
        /// @note The handle is non-owning and backend-defined; use it only for native interop.
        [[nodiscard]] virtual Ptr<void> getNativeHandle() const noexcept = 0;

        /// @brief Returns the native handle cast to a pointer type.
        /// @tparam T Pointer type expected by the native API (for example HWND).
        /// @return Native handle cast to @p T, or null.
        /// @note No type validation is performed; the caller must know the backend.
        template <typename T>
        [[nodiscard]] T getHandle() const noexcept
        {
            static_assert(std::is_pointer_v<T>, "IWindow::getHandle<T>() requires a pointer type");
            return static_cast<T>(getNativeHandle().get());
        }

        /// @brief Returns the window class that produced this window.
        /// @return Non-owning class pointer; the manager owns the class.
        [[nodiscard]] virtual Ptr<const IWindowClass> getWindowClass() const noexcept = 0;

        /// @brief Returns the manager that owns this window.
        /// @return Non-owning platform-service pointer.
        /// @note The manager's mutability is independent of window constness.
        [[nodiscard]] virtual Ptr<IWindowManager> getWindowManager() const noexcept = 0;

        /// @brief Returns the parent window, or null for a top-level window.
        /// @return Parent window pointer, or null.
        [[nodiscard]] virtual Ptr<const IWindow> getParent() const noexcept = 0;

        /// @brief Returns the native window kind.
        /// @return Window type enum value.
        [[nodiscard]] virtual WindowType getType() const noexcept = 0;

        /// @brief Returns the behavioral flags of the window.
        /// @return Window flags bitmask.
        [[nodiscard]] virtual WindowFlags getFlags() const noexcept = 0;

        /// @brief Returns the current title.
        /// @return Reference valid until the next setTitle() call or destruction.
        [[nodiscard]] virtual Link<const std::string> getTitle() const noexcept = 0;

        /// @brief Returns the current window rectangle in screen coordinates.
        /// @return Window rectangle in screen coordinates.
        [[nodiscard]] virtual Math::Shapes::Rect<int32_t> getRect() const noexcept = 0;

        /// @brief Returns the current window client size.
        /// @return Window client size.
        [[nodiscard]] virtual Math::Shapes::Rect<int32_t> getClientSize() const noexcept = 0;

        /// @brief Returns the visual style currently attached to the window.
        /// @return Non-owning pointer; may be null when the class/default style is used.
        [[nodiscard]] virtual Ptr<const WindowStyleDesc> getStyle() const noexcept = 0;

        /// @brief True when the window is enabled and can receive input.
        /// @return True when the window is enabled.
        [[nodiscard]] virtual bool IsEnabled() const = 0;

        /// @brief True when the window is visible.
        /// @return True when the window is visible.
        [[nodiscard]] virtual bool IsVisible() const = 0;

        /// @brief True when the window is minimized.
        /// @return True when the window is minimized.
        [[nodiscard]] virtual bool IsMinimized() const = 0;

        /// @brief True when the window is maximized.
        /// @return True when the window is maximized.
        [[nodiscard]] virtual bool IsMaximized() const = 0;

        /// @brief True when the window currently has keyboard focus.
        /// @return True when the window has focus.
        [[nodiscard]] virtual bool IsFocused() const = 0;

        /// @brief Changes the window title.
        /// @param title New title; copied by the window.
        virtual void setTitle(Link<const std::string> title) = 0;

        /// @brief Moves and resizes the window.
        /// @param rect New rectangle in native screen coordinates.
        virtual void setRect(Link<const Math::Shapes::Rect<int32_t>> rect) = 0;

        /// @brief Replaces the visual style attached to the window.
        /// @param styleDesc Non-owning style pointer; the caller keeps it alive.
        /// @note The backend may defer applying the style until the next frame.
        virtual void setStyle(Ptr<WindowStyleDesc> styleDesc) = 0;

        /// @brief Shows or hides the window.
        /// @param visible True to show, false to hide.
        virtual void setVisible(bool visible) = 0;

        /// @brief Minimizes or restores the window.
        /// @param value True to minimize, false to restore.
        virtual void setMinimized(bool value) = 0;

        /// @brief Maximizes or restores the window.
        /// @param value True to maximize, false to restore.
        virtual void setMaximized(bool value) = 0;

        /// @brief Restores the window to its normal state.
        virtual void Restore() = 0;

        /// @brief Brings the window to the foreground and top of the Z order.
        virtual void BringToFront() = 0;

        /// @brief Requests attention by flashing the window.
        /// @param count Number of flashes; 0 is treated as one flash by some backends.
        /// @param timeoutMs Flash period in milliseconds; 0 lets the backend choose.
        virtual void Flash(uint32_t count, uint32_t timeoutMs) = 0;

        /// @brief Flushes pending paint work for the window.
        virtual void Update() = 0;

        /// @brief Recalculates the native non-client frame after a style/state change.
        virtual void FrameChanged() = 0;

        /// @brief Makes a newly created window visible and fully initialized.
        /// @note Typically shows the window, applies frame changes and flushes paint.
        virtual void Establish() = 0;

        /// @brief Destroys the native window handle.
        /// @note The manager may need to process the resulting native destroy
        ///       event before the window is removed from its list.
        virtual void Destroy() = 0;
    };

    must_be_abstract(IWindow)
    must_provide_native_handle(IWindow)

    /// @brief A registered native window class shared by windows of the same kind.
    ///
    /// The class fixes the native window procedure and the default type, flags
    /// and style. Window class objects are owned by IWindowManager and exposed
    /// through non-owning pointers.
    class API IWindowClass // NOLINT
    {
    public:
        virtual ~IWindowClass() = default;

        /// @brief Returns the unique registered class name.
        /// @return Class name string view.
        [[nodiscard]] virtual std::string_view getName() const noexcept = 0;

        /// @brief Returns the base window type used by the class.
        /// @return Window type enum value.
        [[nodiscard]] virtual WindowType getBaseType() const noexcept = 0;

        /// @brief Returns the base behavioral flags used by the class.
        /// @return Window flags bitmask.
        [[nodiscard]] virtual WindowFlags getBaseFlags() const noexcept = 0;

        /// @brief Returns the event procedure installed for the class.
        /// @return Non-owning pointer; may be null for a default class.
        [[nodiscard]] virtual Ptr<const Events::IEventProc> getEventProc() const noexcept = 0;

        /// @brief Returns the default visual style for windows of this class.
        /// @return Non-owning pointer; may be null.
        [[nodiscard]] virtual Ptr<WindowStyleDesc> getBaseStyle() const noexcept = 0;
    };

    must_be_abstract(IWindowClass)

    /// @brief Owns window classes and windows and pumps native messages.
    ///
    /// The manager is a platform service created by IPlatform. It owns every
    /// WindowClassDesc/WindowDesc passed to RegisterWindowClass()/MakeWindow()
    /// and every resulting object. Returned pointers are non-owning and remain
    /// valid until the object is destroyed through the manager.
    class API IWindowManager // NOLINT
    {
    public:
        virtual ~IWindowManager() = default;

        /// @brief Registers a native window class.
        /// @param desc Class description; the manager takes ownership.
        /// @return Non-owning pointer to the registered class, or null on failure.
        [[nodiscard]] virtual Ptr<const IWindowClass> RegisterWindowClass(
            UniquePtr<WindowClassDesc> desc) = 0;

        /// @brief Creates a window from a description.
        /// @param desc Fully populated description; the manager takes ownership.
        /// @return Non-owning pointer to the created window, or null on failure.
        /// @note The description must reference a window class registered with
        ///       this manager through RegisterWindowClass().
        [[nodiscard]] virtual Ptr<IWindow> MakeWindow(
            UniquePtr<WindowDesc> desc) = 0;

        /// @brief Returns the currently active/focused window, or null.
        /// @return Active window pointer, or null.
        [[nodiscard]] virtual Ptr<const IWindow> getActiveWindow() const noexcept = 0;

        /// @brief Returns the application's main window, or null.
        /// @return Main window pointer, or null.
        [[nodiscard]] virtual Ptr<const IWindow> getMainWindow() const noexcept = 0;

        /// @brief Returns the number of live windows owned by the manager.
        /// @return Number of live windows.
        [[nodiscard]] virtual size_t getWindowCount() const noexcept = 0;

        /// @brief Returns the owned windows as a non-owning view.
        /// @return Span valid until the next window creation/destruction.
        /// @note The span exposes ownership wrappers; callers must not modify,
        ///       move or remove entries.
        [[nodiscard]] virtual std::span<const UniquePtr<IWindow>> getWindowList() const noexcept = 0;

        /// @brief Lets the manager observe an event before regular subscribers.
        /// @param manager Event manager currently dispatching the event.
        /// @param ctx Context of the current dispatch.
        /// @param e Mutable event; the manager may inspect it and mark it handled.
        virtual void ProcessEvent(Ptr<const Events::IEventManager> manager,
                                  Link<const Events::EventContext> ctx,
                                  Link<const Events::Event> e) = 0;

        /// @brief Pumps all pending native messages for managed windows.
        /// @note Does not block; call once per frame or whenever input is needed.
        virtual void ProcessEvents() = 0;

        /// @brief Queues a window for destruction.
        /// @param window Window to destroy; destruction is deferred until FlushDestroyQueue().
        /// @note Requests are queued because destroying a window can dispatch
        ///       further native events that re-enter the manager.
        virtual void RequestDestroy(Ptr<const IWindow> window) = 0;

        /// @brief Processes queued destruction requests and removes the windows.
        virtual void FlushDestroyQueue() = 0;

        /// @brief Calls Update() on every live window.
        virtual void UpdateWindows() = 0;

        /// @brief Advances one frame of window maintenance.
        /// @note Currently updates windows and flushes the destroy queue; call
        ///       once per frame after event processing.
        virtual void Tick() = 0;
    };

    must_be_abstract(IWindowManager)
}
