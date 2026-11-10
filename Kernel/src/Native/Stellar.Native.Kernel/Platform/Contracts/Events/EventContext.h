// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Events/EventContext.h
/// @brief Per-dispatch metadata supplied to event handlers.
#pragma once

#include "Platform/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Platform::Events
{
    /// @brief Context describing one event-dispatch call.
    ///
    /// A context is created by the event procedure or event manager for every
    /// dispatch. It identifies the manager currently running, the original
    /// sender and the frame/time at which the event is processed. Handlers must
    /// not retain references to context fields after returning.
    struct API EventContext final
    {
        Ptr<const IEventManager> Manager = nullptr; ///< Manager currently dispatching the event; may be null for synthetic calls.
        Ptr<const void> Sender = nullptr;           ///< Original sender of the event, usually the source window.
        uint64_t Frame = 0u;                        ///< Engine frame number at dispatch time, or 0 when unavailable.
        double RealTime = 0.0;                      ///< Seconds elapsed in real time since application start.
        double SimTime = 0.0;                       ///< Seconds elapsed in simulation time, which may be paused or scaled.
        uint32_t ThreadId = 0u;                     ///< Native thread identifier that dispatched the event, or 0.
        uint32_t Depth = 0u;                        ///< Recursion depth of nested dispatches; 0 for a top-level event.
    };
}
