// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Events/EventDelegate.h
/// @brief Lightweight type-erased event handler.
#pragma once

#include "Platform/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Platform::Events
{
    /// @brief Non-owning callable wrapper used for event subscriptions.
    ///
    /// A delegate stores a function pointer and an optional user/instance
    /// pointer. It is trivially copyable and exactly two pointers wide, so it
    /// can be stored in subscription lists without allocation. The delegate
    /// does not own the instance passed to Member(); the instance must outlive
    /// the subscription.
    ///
    /// The callable returns true when it consumed the event. Returning true
    /// stops dispatch to later handlers and marks the event as handled.
    class API EventDelegate final
    {
        /// @brief Signature of the type-erased invocation thunk.
        using InvokeFn = bool(*)(Link<const EventContext>, Link<const Event>, Ptr<void>);

        InvokeFn _fn = nullptr; ///< Thunk that adapts the concrete callable.
        Ptr<void> _user = nullptr; ///< Instance pointer for member functions; null for free functions.

        /// @brief Creates a delegate from a thunk and an optional instance.
        /// @param fn Invocation thunk; must be non-null.
        /// @param user Instance passed to the thunk, or null.
        constexpr EventDelegate(InvokeFn fn, Ptr<void> user) noexcept
            : _fn(fn),
              _user(user) {}

        /// @brief Thunk for a free function with the event-handler signature.
        /// @tparam Fn Function pointer to invoke.
        template <auto Fn>
        static bool FreeThunk(Link<const EventContext> ctx, Link<const Event> e, Ptr<void>) noexcept
        {
            static_assert(std::is_pointer_v<decltype(Fn)>, "EventDelegate::Free requires a function");
            return Fn(ctx, e);
        }

        /// @brief Thunk that invokes a member function on a stored instance.
        /// @tparam Method Member-function pointer to invoke.
        /// @tparam T Concrete class that declares the member function.
        template <auto Method, class T>
        static bool MemberThunk(Link<const EventContext> ctx, Link<const Event> e, Ptr<void> user) noexcept
        {
            static_assert(std::is_member_function_pointer_v<decltype(Method)>,
                          "EventDelegate::Member requires a member function");
            static_assert(std::is_class_v<T>, "EventDelegate::Member requires a class instance");
            return (user.As<T>()->*Method)(ctx, e);
        }

    public:
        /// @brief Creates a delegate from a free function.
        /// @tparam Fn Function pointer with signature
        ///         `bool(const EventContext&, const Event&)`.
        /// @return Delegate that invokes @p Fn.
        /// @note The function must be stateless with respect to the delegate;
        ///       use Member() when handler state is required.
        template <auto Fn>
        [[nodiscard]] static constexpr EventDelegate Free() noexcept
        {
            return EventDelegate(&FreeThunk<Fn>, nullptr);
        }

        /// @brief Creates a delegate bound to a member function of @p instance.
        /// @tparam Method Member-function pointer with signature
        ///         `bool(const EventContext&, const Event&)`.
        /// @tparam T Concrete class that declares @p Method.
        /// @param instance Non-owning pointer to the object that will receive the call.
        /// @return Delegate that invokes `instance->Method(ctx, e)`.
        /// @note The instance must remain alive for as long as the delegate is
        ///       subscribed. The delegate does not extend its lifetime.
        template <auto Method, class T>
        [[nodiscard]] static constexpr EventDelegate Member(Ptr<T> instance) noexcept
        {
            static_assert(std::is_member_function_pointer_v<decltype(Method)>,
                          "EventDelegate::Member requires a member function");
            static_assert(std::is_class_v<T>, "EventDelegate::Member requires a class instance");
            return EventDelegate(&MemberThunk<Method, T>, instance);
        }

        /// @brief Invokes the delegate.
        /// @param ctx Context of the current dispatch.
        /// @param e Event being dispatched.
        /// @return True when the handler consumed the event; false for an empty delegate.
        [[nodiscard]] constexpr bool operator()(Link<const EventContext> ctx, Link<const Event> e) const noexcept
        {
            return _fn ? _fn(ctx, e, _user) : false;
        }

        /// @brief True when the delegate has a callable target.
        /// @return True when a callable is bound.
        [[nodiscard]] constexpr explicit operator bool() const noexcept
        {
            return _fn != nullptr;
        }

        /// @brief Two delegates are equal when both the callable and instance match.
        [[nodiscard]] friend constexpr bool operator==(EventDelegate a, EventDelegate b) noexcept
        {
            return a._fn == b._fn && a._user == b._user;
        }
    };

    // Keeps the delegate cheap to copy inside subscription containers.
    static_assert(sizeof(EventDelegate) == sizeof(void*) * 2,
                  "EventDelegate must stay a two-pointer value type");
    static_assert(std::is_trivially_copyable_v<EventDelegate>,
                  "EventDelegate must be trivially copyable");
}
