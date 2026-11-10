// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Events/Interfaces.h
/// @brief Event subscription, translation and native-dispatch interfaces.
#pragma once

#include <span>

#include "Platform/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Platform::Events
{
    /// @brief Stores subscriptions and dispatches events to matching handlers.
    ///
    /// A manager owns its subscription list but not the delegates' instances.
    /// Dispatching is synchronous: handlers run on the thread that calls
    /// Process() and may subscribe or unsubscribe during the call. Handlers
    /// return true to consume an event, which stops further dispatch.
    class API IEventManager // NOLINT
    {
    public:
        virtual ~IEventManager() = default;

        /// @brief Subscribes a handler to an event type.
        /// @param type Event type to receive, or EventType::Unknown to receive all events.
        /// @param handler Callback to invoke; the manager stores a copy of the delegate.
        /// @note The delegate does not own its instance; keep the target alive
        ///       until the subscription is removed or the manager is cleared.
        virtual void Subscribe(EventType type, EventDelegate handler) = 0;

        /// @brief Removes subscriptions matching @p handler.
        /// @param handler Delegate to compare by thunk and instance pointer.
        virtual void Unsubscribe(EventDelegate handler) = 0;

        /// @brief Removes every subscription from the manager.
        virtual void Clear() = 0;

        /// @brief Dispatches an event with an explicit context.
        /// @param ctx Context describing the current dispatch.
        /// @param e Event to dispatch.
        /// @return True when a handler consumed the event and propagation should stop.
        /// @note The source window manager, when present, is given a chance to
        ///       process the event before regular subscribers.
        virtual bool Process(Link<const EventContext> ctx, Link<Event> e) const = 0;

        /// @brief Dispatches an event using a context derived from the event source.
        /// @param e Event to dispatch.
        /// @return True when a handler consumed the event.
        virtual bool Process(Link<Event> e) const = 0;
    };

    must_be_abstract(IEventManager)

    /// @brief Converts backend-native messages into kernel Event values.
    ///
    /// Translators are backend-specific and stateless with respect to the
    /// event stream. The returned Event carries a payload alternative matching
    /// its EventType; unsupported native messages should be reported as
    /// EventType::Unknown with UnknownPayload rather than throwing.
    class API IEventTranslator // NOLINT
    {
    public:
        virtual ~IEventTranslator() = default;

        /// @brief Translates one native event into a kernel Event.
        /// @param window Window that received the native message; may be null.
        /// @param nativeType Backend-native message/event code.
        /// @param params Raw native parameters associated with the message.
        /// @return Translated event. The source window and payload are filled
        ///         according to the message; unknown messages produce
        ///         EventType::Unknown.
        /// @note The method is noexcept; it must not throw on malformed native data.
        [[nodiscard]] virtual Event Translate(
            Ptr<const Window::IWindow> window,
            uint32_t nativeType,
            Link<const EventParams> params
        ) const noexcept = 0;
    };

    must_be_abstract(IEventTranslator)

    /// @brief Native event entry point that fans messages out to managers.
    ///
    /// The platform backend owns one event procedure. It receives native
    /// messages, translates them through IEventTranslator and dispatches the
    /// resulting Event to registered managers in order until one consumes it.
    class API IEventProc // NOLINT
    {
    public:
        virtual ~IEventProc() = default;

        /// @brief Registers an event manager for native dispatch.
        /// @param manager Non-owning manager to add; duplicate registrations are ignored.
        /// @note The manager must remain alive until RemoveManager() is called or
        ///       the event procedure is destroyed.
        virtual void AddManager(Ptr<const IEventManager> manager) = 0;

        /// @brief Removes a previously registered manager.
        /// @param manager Manager to remove; removing an unknown manager is a no-op.
        virtual void RemoveManager(Ptr<const IEventManager> manager) = 0;

        /// @brief Returns the number of registered managers.
        /// @return Number of registered managers.
        [[nodiscard]] virtual size_t getManagerCount() const noexcept = 0;

        /// @brief Returns the registered managers as a non-owning view.
        /// @return Span valid until the next AddManager()/RemoveManager() call.
        [[nodiscard]] virtual std::span<const Ptr<const IEventManager>> getManagerList() const noexcept = 0;

        /// @brief Processes one native event.
        /// @param window Window that received the message; may be null.
        /// @param nativeType Backend-native message/event code.
        /// @param params Raw native parameters associated with the message.
        /// @return Backend-specific result passed to the native message loop
        ///         (LRESULT on Windows). Handled events normally return 0; other
        ///         values come from the backend's default procedure.
        virtual intptr_t Proc(Ptr<const Window::IWindow> window,
                              uint32_t nativeType,
                              Link<const EventParams> params) const = 0;
    };

    must_be_abstract(IEventProc)
}
