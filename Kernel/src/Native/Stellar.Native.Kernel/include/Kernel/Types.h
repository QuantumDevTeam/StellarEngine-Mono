// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/Types.h
/// @brief Smart-pointer vocabulary types shared by the whole engine.
///
/// The header defines the owning and non-owning pointer wrappers used in
/// public interfaces: Ptr, Link, UniquePtr, SharedPtr and WeakPtr.
#pragma once

#include <cstddef>
#include <cstdint>
#include <memory>
#include <type_traits>

#include "macro.h"

/// @brief Pointer wrappers and ownership aliases used across the engine.
namespace Stellar::Native::Types
{
    // Forward declaration of the non-owning pointer wrapper. The class itself
    // is documented at its definition below; keeping this comment non-Doxygen
    // prevents Doxygen from merging the brief and @tparam twice.
    template <typename T>
    class Ptr;

    /// @brief Trait that detects Ptr specializations.
    ///
    /// The primary template is false; the Ptr<T> specialization is true.
    /// Used to disambiguate the reference constructor from Ptr arguments.
    ///
    /// @tparam T Type to inspect.
    template <typename T>
    struct IsPtr : std::false_type {};

    /// @brief Ptr<T> is recognized as a pointer wrapper.
    /// @tparam T Pointee type.
    template <typename T>
    struct IsPtr<Ptr<T>> : std::true_type {};

    // Ownership and const rules used across the engine:
    //
    //   UniquePtr<T>     exclusive ownership; pass by value to transfer it
    //   SharedPtr<T>     shared ownership; only when lifetime is genuinely shared
    //   Ptr<T>           non-owning, nullable observer (lightweight wrapper)
    //   Link<T>          non-owning, non-nullable reference (alias for T&)
    //   Ptr<const T>     pointer to read-only data
    //   const Ptr<T>     const pointer value (the pointee is still mutable)
    //
    // Prefer Link<const T> for read-only parameters and Ptr<const T> for
    // read-only return values. Raw T*/T& are reserved for low-level native
    // APIs, copy/move operations and pointer arithmetic. Native handles
    // (HWND, ID3D11Device*, mapped memory, ...) stay raw because they belong
    // to the platform API and are never owned by the engine.

    /// @brief Non-owning, nullable pointer with checked conversions.
    ///
    /// Zero-overhead wrapper around `T*`. It is trivially copyable,
    /// standard-layout and pointer-sized. Use it for observers, optional
    /// references and return values that may be null; use Link for references
    /// that must not be null. The wrapper never owns or destroys the pointee.
    ///
    /// @tparam T Pointee type; may be `const` or `void`.
    template <typename T>
    class Ptr final
    {
        static_assert(!std::is_reference_v<T>, "Ptr<T> cannot point to a reference type");
        static_assert(!std::is_function_v<T>, "Ptr<T> cannot point to a function type");

        T* _ptr = nullptr;

    public:
        /// @brief Type of the pointee.
        using element_type = T;

        /// @brief Creates a null pointer.
        constexpr Ptr() noexcept = default;

        /// @brief Creates a null pointer from `nullptr`.
        constexpr Ptr(std::nullptr_t) noexcept {}

        /// @brief Wraps a raw pointer without taking ownership.
        /// @param ptr Pointer to observe; may be null.
        constexpr Ptr(T* ptr) noexcept :
            _ptr(ptr) {}

        /// @brief Wraps a raw uintptr pointer without taking ownership.
        /// @param ptr Pointer to observe; may be null.
        constexpr Ptr(uintptr_t ptr) noexcept :
            _ptr(reinterpret_cast<T*>(ptr)) {}

        /// @brief Creates a pointer to an existing object.
        ///
        /// Disabled for pointer-like arguments and Ptr values so that overload
        /// resolution picks the pointer or converting constructor instead.
        ///
        /// @tparam U Deduced type of the object; defaults to @p T.
        /// @param ptr Object to point at; must outlive the Ptr.
        template <typename U = T> requires (!std::is_void_v<U>
                                            && !std::is_pointer_v<std::remove_cvref_t<U>>
                                            && !IsPtr<std::remove_cvref_t<U>>::value)
        constexpr Ptr(U& ptr) noexcept :
            _ptr(&ptr) {}

        /// @brief Converting constructor from a related Ptr.
        /// @tparam U Source pointee type, convertible to @p T.
        /// @param other Source pointer; may be null.
        template <typename U> requires (!std::is_same_v<U, T>
                                        && std::is_convertible_v<U*, T*>)
        constexpr Ptr(Ptr<U> other) noexcept :
            _ptr(other.get()) {}

        /// @brief Copies the pointer value.
        constexpr Ptr(const Ptr&) noexcept = default;
        /// @brief Moves the pointer value; the source remains valid but is not cleared.
        constexpr Ptr(Ptr&&) noexcept = default;
        /// @brief Copies the pointer value.
        /// @return Reference to this pointer.
        constexpr Ptr& operator=(const Ptr&) noexcept = default;
        /// @brief Moves the pointer value.
        /// @return Reference to this pointer.
        constexpr Ptr& operator=(Ptr&&) noexcept = default;

        /// @brief Resets the pointer to null.
        /// @return Reference to this pointer.
        constexpr Ptr& operator=(std::nullptr_t) noexcept
        {
            _ptr = nullptr;
            return *this;
        }

        /// @brief Replaces the observed pointer.
        /// @param ptr New pointer to observe; may be null.
        /// @return Reference to this pointer.
        constexpr Ptr& operator=(T* ptr) noexcept
        {
            _ptr = ptr;
            return *this;
        }

        /// @brief Replaces the observed pointer from a related Ptr.
        /// @tparam U Source pointee type, convertible to @p T.
        /// @param other Source pointer; may be null.
        /// @return Reference to this pointer.
        template <typename U> requires (!std::is_same_v<U, T>
                                        && std::is_convertible_v<U*, T*>)
        constexpr Ptr& operator=(Ptr<U> other) noexcept
        {
            _ptr = other.get();
            return *this;
        }

        /// @brief Returns the observed raw pointer.
        /// @return Raw pointer, possibly null.
        [[nodiscard]] constexpr T* get() const noexcept { return _ptr; }

        /// @brief Member access through the observed pointer.
        /// @return Pointer used for `->`; must not be null.
        [[nodiscard]] constexpr T* operator->() const noexcept requires (!std::is_void_v<T>)
        {
            return _ptr;
        }

        /// @brief Dereferences the observed pointer.
        /// @tparam U Deduced pointee type; defaults to @p T.
        /// @return Reference to the pointee; the pointer must not be null.
        template <typename U = T> requires (!std::is_void_v<U>)
        [[nodiscard]] constexpr U& operator*() const noexcept
        {
            return *_ptr;
        }

        /// @brief Tests whether the pointer is non-null.
        /// @return True when a pointee is present.
        [[nodiscard]] constexpr explicit operator bool() const noexcept { return _ptr != nullptr; }

        /// @brief Implicitly converts to the raw pointer.
        /// @return Raw pointer, possibly null.
        [[nodiscard]] constexpr operator T*() const noexcept { return _ptr; }

        /// @brief Explicitly converts to a related pointer type.
        ///
        /// Upcasts, downcasts and `void*` conversions are permitted when the
        /// compiler accepts the static cast; removing constness is rejected at
        /// compile time.
        ///
        /// @tparam U Target pointee type.
        /// @return Pointer viewing the same address as @p U.
        template <typename U>
        [[nodiscard]] constexpr Ptr<U> As() const noexcept
        {
            static_assert(!std::is_const_v<T> || std::is_const_v<U>,
                          "Ptr::As<U>() cannot remove constness");
            static_assert(std::is_void_v<T> || std::is_void_v<U> ||
                          std::is_base_of_v<std::remove_cv_t<T>, std::remove_cv_t<U>> ||
                          std::is_base_of_v<std::remove_cv_t<U>, std::remove_cv_t<T>> ||
                          std::is_same_v<std::remove_cv_t<T>, std::remove_cv_t<U>>,
                          "Ptr::As<U>() requires related or void pointer types");
            return Ptr<U>(static_cast<U*>(_ptr));
        }

        /// @brief Resets the pointer to null.
        constexpr void Reset() noexcept { _ptr = nullptr; }
    };

    /// @brief Deduction guide: `Ptr(raw)` deduces `Ptr<T>`.
    /// @tparam T Pointee type deduced from the argument.
    template <typename T>
    Ptr(T*) -> Ptr<T>;

    /// @brief Non-owning, non-nullable reference alias.
    ///
    /// Prefer Link<T> over `T&` in public interfaces to make the ownership
    /// contract explicit. A Link must always refer to a valid object.
    ///
    /// @tparam T Referenced type; may be `const`.
    template <typename T>
    using Link = T&;

    /// @brief Exclusive-ownership pointer alias for `std::unique_ptr`.
    /// @tparam T Owned type.
    template <typename T>
    using UniquePtr = std::unique_ptr<T>;

    /// @brief Shared-ownership pointer alias for `std::shared_ptr`.
    /// @tparam T Owned type.
    template <typename T>
    using SharedPtr = std::shared_ptr<T>;

    /// @brief Non-owning weak observer alias for `std::weak_ptr`.
    /// @tparam T Observed type.
    template <typename T>
    using WeakPtr = std::weak_ptr<T>;

    /// @brief Free-function form of `Ptr::get`.
    /// @tparam T Pointee type.
    /// @param p Pointer to inspect.
    /// @return Raw pointer, possibly null.
    template <typename T>
    [[nodiscard]] constexpr T* Get(Ptr<T> p) noexcept { return p.get(); }

    /// @brief Dereferences a Ptr.
    /// @tparam T Pointee type.
    /// @param p Pointer to dereference; must not be null.
    /// @return Reference to the pointee.
    template <typename T>
    [[nodiscard]] constexpr T& Deref(Ptr<T> p) noexcept
    {
        return *p.get();
    }

    /// @brief Tests whether a Ptr is null.
    /// @tparam T Pointee type.
    /// @param p Pointer to inspect.
    /// @return True when no pointee is present.
    template <typename T>
    [[nodiscard]] constexpr bool IsNull(Ptr<T> p) noexcept { return p.get() == nullptr; }

    /// @brief Tests whether a Ptr is non-null.
    /// @tparam T Pointee type.
    /// @param p Pointer to inspect.
    /// @return True when a pointee is present.
    template <typename T>
    [[nodiscard]] constexpr bool IsNotNull(Ptr<T> p) noexcept { return p.get() != nullptr; }

    /// @brief Free-function form of `Ptr::As`.
    /// @tparam U Target pointee type.
    /// @tparam T Source pointee type.
    /// @param p Pointer to convert.
    /// @return Pointer viewing the same address as @p U.
    template <typename U, typename T>
    [[nodiscard]] constexpr Ptr<U> As(Ptr<T> p) noexcept
    {
        static_assert(!std::is_const_v<T> || std::is_const_v<U>,
                      "As<U>() cannot remove constness");
        static_assert(std::is_void_v<T> || std::is_void_v<U> ||
                      std::is_base_of_v<std::remove_cv_t<T>, std::remove_cv_t<U>> ||
                      std::is_base_of_v<std::remove_cv_t<U>, std::remove_cv_t<T>> ||
                      std::is_same_v<std::remove_cv_t<T>, std::remove_cv_t<U>>,
                      "As<U>() requires related or void pointer types");
        return Ptr<U>(static_cast<U*>(p.get()));
    }

    /// @brief Returns the address of a Link.
    /// @tparam T Referenced type.
    /// @param l Reference to inspect.
    /// @return Pointer to the referenced object; never null.
    template <typename T>
    [[nodiscard]] constexpr T* Get(Link<T> l) noexcept { return &l; }

    /// @brief Returns a Link unchanged.
    /// @tparam T Referenced type.
    /// @param l Reference to return.
    /// @return Reference to the same object.
    template <typename T>
    [[nodiscard]] constexpr T& Deref(Link<T> l) noexcept
    {
        return l;
    }

    /// @brief Converts a Link to a Ptr of a related type.
    /// @tparam U Target pointee type.
    /// @tparam T Source referenced type.
    /// @param l Reference to convert; must be non-null by construction.
    /// @return Pointer viewing the referenced object as @p U.
    template <typename U, typename T>
    [[nodiscard]] constexpr Ptr<U> As(Link<T> l) noexcept
    {
        static_assert(!std::is_const_v<T> || std::is_const_v<U>,
                      "As<U>() cannot remove constness");
        static_assert(std::is_void_v<T> || std::is_void_v<U> ||
                      std::is_base_of_v<std::remove_cv_t<T>, std::remove_cv_t<U>> ||
                      std::is_base_of_v<std::remove_cv_t<U>, std::remove_cv_t<T>> ||
                      std::is_same_v<std::remove_cv_t<T>, std::remove_cv_t<U>>,
                      "As<U>() requires related or void pointer types");
        return Ptr<U>(static_cast<U*>(&l));
    }

    /// @brief Compares two Ptr values by address.
    /// @tparam T Left pointee type.
    /// @tparam U Right pointee type.
    /// @param lhs Left pointer.
    /// @param rhs Right pointer.
    /// @return True when both observe the same address.
    template <typename T, typename U>
    [[nodiscard]] constexpr bool operator==(Ptr<T> lhs, Ptr<U> rhs) noexcept
    {
        return lhs.get() == rhs.get();
    }

    /// @brief Compares two Ptr values by address.
    /// @tparam T Left pointee type.
    /// @tparam U Right pointee type.
    /// @param lhs Left pointer.
    /// @param rhs Right pointer.
    /// @return True when the pointers observe different addresses.
    template <typename T, typename U>
    [[nodiscard]] constexpr bool operator!=(Ptr<T> lhs, Ptr<U> rhs) noexcept
    {
        return !(lhs == rhs);
    }

    /// @brief Tests a Ptr against `nullptr`.
    /// @tparam T Pointee type.
    /// @param lhs Pointer to test.
    /// @return True when the pointer is null.
    template <typename T>
    [[nodiscard]] constexpr bool operator==(Ptr<T> lhs, std::nullptr_t) noexcept
    {
        return lhs.get() == nullptr;
    }

    /// @brief Tests `nullptr` against a Ptr.
    /// @tparam T Pointee type.
    /// @param rhs Pointer to test.
    /// @return True when the pointer is null.
    template <typename T>
    [[nodiscard]] constexpr bool operator==(std::nullptr_t, Ptr<T> rhs) noexcept
    {
        return rhs.get() == nullptr;
    }

    /// @brief Tests a Ptr against `nullptr` for inequality.
    /// @tparam T Pointee type.
    /// @param lhs Pointer to test.
    /// @return True when the pointer is non-null.
    template <typename T>
    [[nodiscard]] constexpr bool operator!=(Ptr<T> lhs, std::nullptr_t) noexcept
    {
        return lhs.get() != nullptr;
    }

    /// @brief Tests `nullptr` against a Ptr for inequality.
    /// @tparam T Pointee type.
    /// @param rhs Pointer to test.
    /// @return True when the pointer is non-null.
    template <typename T>
    [[nodiscard]] constexpr bool operator!=(std::nullptr_t, Ptr<T> rhs) noexcept
    {
        return rhs.get() != nullptr;
    }

    // Compile-time assertions
    static_assert(sizeof(Ptr<int>) == sizeof(void*), "Ptr must stay pointer-sized");
    static_assert(alignof(Ptr<int>) == alignof(void*), "Ptr must keep pointer alignment");
    static_assert(std::is_trivially_copyable_v<Ptr<int>>, "Ptr must be trivially copyable");
    static_assert(std::is_standard_layout_v<Ptr<int>>, "Ptr must be standard-layout");
    static_assert(std::is_convertible_v<Ptr<int>, Ptr<const int>>,
                  "Ptr<T> must convert to Ptr<const T>");
    static_assert(!std::is_convertible_v<Ptr<const int>, Ptr<int>>,
                  "Ptr<const T> must not convert to Ptr<T>");
    static_assert(std::is_same_v<decltype(Ptr<int>().As<const int>()), Ptr<const int>>,
                  "As<const T>() must preserve type");
    static_assert(!std::is_constructible_v<Ptr<int>, Ptr<const int>>,
                  "Ptr<T> must not be constructible from Ptr<const T>");
}
