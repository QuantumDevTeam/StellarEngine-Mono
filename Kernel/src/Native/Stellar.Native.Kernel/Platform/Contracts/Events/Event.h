// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Events/Event.h
/// @brief Translated platform event and typed payload accessors.
#pragma once

#include "Platform/Types.h" // NOLINT

#include "EventFlags.h"
#include "EventTypes.h"
#include "EventPayloads.h"

namespace Stellar::Native::Kernel::Platform::Events
{
    /// @brief One translated event: kind, flags, source and payload.
    ///
    /// Events are passed by const reference during dispatch and are cheap
    /// value objects. The source pointer is non-owning and identifies the
    /// window that produced the event; it may be null for application-wide
    /// notifications. Handlers inspect the payload with TryGet()/Get() and
    /// mark the event as consumed with setAsHandled().
    struct API Event final
    {
        EventType Type = EventType::Unknown;      ///< Semantic event kind; determines the expected payload.
        EventFlags Flags = EventFlags::None;      ///< Origin, repeat and handled flags.
        Ptr<const Window::IWindow> Source = nullptr; ///< Window that produced the event, or null.
        EventPayload Payload{};                   ///< Typed payload; std::monostate when there is no data.

        /// @brief Returns the payload when it holds @p P, otherwise null.
        /// @tparam P Payload type to query.
        /// @return Pointer to the stored payload, or null on type mismatch.
        /// @note The pointer is valid only while this Event is alive.
        template <class P>
        [[nodiscard]] constexpr Ptr<const P> TryGet() const noexcept
        {
            return std::get_if<P>(&Payload);
        }

        /// @brief Returns the payload as @p P.
        /// @tparam P Payload type to retrieve.
        /// @return Const reference to the stored payload.
        /// @throws std::bad_variant_access When the active alternative is not @p P.
        /// @note The reference is valid only while this Event is alive.
        template <class P>
        [[nodiscard]] constexpr Link<const P> Get() const
        {
            return std::get<P>(Payload);
        }

        /// @brief True when a handler has already consumed the event.
        /// @return True when the Handled flag is set.
        [[nodiscard]] constexpr bool isHandled() const noexcept
        {
            return HasFlag(Flags, EventFlags::Handled);
        }

        /// @brief Marks the event as consumed so later handlers can skip it.
        /// @note This only sets the flag; the dispatcher decides whether to
        ///       stop propagation.
        constexpr void setAsHandled() noexcept
        {
            Flags |= EventFlags::Handled;
        }
    };

    // Keeps the value small enough to pass by const reference without
    // accidental copies becoming a hot-path cost.
    static_assert(sizeof(Event) <= 80, "Event budget exceeded — review payload variant");
}
