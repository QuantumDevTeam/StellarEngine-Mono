// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Backend/Interfaces.h
/// @brief Platform service interface implemented by every platform backend.
#pragma once

#include <string_view>

#include "Platform/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Platform
{
    /// @brief Process-wide platform service and owner of the default managers.
    ///
    /// A platform backend creates one IPlatform instance (see
    /// Platform::MakePlatform<B>()) and keeps it alive until process exit. The
    /// service owns the default window manager, event manager, event procedure
    /// and event translator; callers receive non-owning pointers to those
    /// services and must not destroy them.
    ///
    /// Backends may expose additional services through their concrete types,
    /// but code that only needs windows and events can work through this
    /// interface.
    class API IPlatform // NOLINT
    {
    public:
        virtual ~IPlatform() = default;

        /// @brief Returns the default window manager owned by this platform.
        /// @return Non-owning pointer to the process-wide window manager.
        /// @note The pointer remains valid for the lifetime of the platform and
        ///       must not be deleted by the caller.
        [[nodiscard]] virtual Ptr<Window::IWindowManager> getDefaultWindowManager() = 0;

        /// @brief Returns the default event manager owned by this platform.
        /// @return Non-owning pointer to the process-wide event manager.
        /// @note Managers are mutable services even when the platform is used
        ///       through a const reference; the returned pointer is non-const.
        [[nodiscard]] virtual Ptr<Events::IEventManager> getDefaultEventManager() = 0;

        /// @brief Returns the native event procedure owned by this platform.
        /// @return Non-owning pointer to the event entry point used by windows.
        /// @note The procedure dispatches messages for every window managed by
        ///       this platform; do not register it with another thread's queue.
        [[nodiscard]] virtual Ptr<Events::IEventProc> getDefaultEventProc() = 0;

        /// @brief Returns the native event translator owned by this platform.
        /// @return Non-owning pointer to the backend-specific translator.
        /// @note Translators do not own windows and must not retain pointers to
        ///       caller-owned EventParams data.
        [[nodiscard]] virtual Ptr<Events::IEventTranslator> getDefaultEventTranslator() = 0;

        /// @brief Returns the backend's native process instance.
        /// @return Opaque native instance handle (for example HINSTANCE on
        ///         Windows), or null when the backend has no such concept.
        /// @note The value is backend-defined; use it only for native interop.
        [[nodiscard]] virtual Ptr<void> getNativeInstance() const noexcept = 0;
        /// @brief Returns a stable, human-readable backend name.
        /// @return Backend name such as "Windows" or "Wayland"; the string is
        ///         owned by the backend and remains valid for its lifetime.
        [[nodiscard]] virtual std::string_view getBackendName() const noexcept = 0;
    };

    must_be_abstract(IPlatform)
}
