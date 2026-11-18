// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Memory/NativeList.h
/// @brief Native lists in C++: the same header and the same lock as Stellar.Core.Memory in C#.
///
/// Header-only and over Abi/Memory.h alone, so a native module uses it without linking the native core: memory goes
/// through the StellarMemoryApi the module is given. It pulls in <atomic>, so a C++/CLI translation unit does not
/// include it.
#pragma once

#include <atomic>
#include <cstring>
#include <span>
#include <thread>
#include <type_traits>

#if defined(_M_X64) || defined(_M_IX86) || defined(__x86_64__) || defined(__i386__)
#include <immintrin.h>
#define STELLAR_CPU_PAUSE() _mm_pause()
#else
#define STELLAR_CPU_PAUSE() ((void)0)
#endif

namespace Stellar::Native::Kernel::Memory
{
    /// @brief The lock of a concurrent native list, as Stellar.Core.Memory.NativeListLock takes it in C#.
    ///
    /// Many readers or one writer. A writer that waits stops new readers, so a stream of readers never starves it.
    /// Not reentrant: a holder that takes it again waits for itself.
    class NativeListLock final
    {
        // Held for nanoseconds, so spin first, longer each time, so that waiters stop hammering the line of the
        // lock; then give the processor away. As the C# side does
        static void Pause(uint32_t spins) noexcept
        {
            if (spins >= 10)
            {
                std::this_thread::yield();
                return;
            }

            for (uint32_t i = 0; i < (1u << spins); ++i)
                STELLAR_CPU_PAUSE();
        }

    public:
        NativeListLock() = delete;

        static void EnterRead(Link<StellarNativeList> list) noexcept
        {
            std::atomic_ref<int32_t> state(list.lock);
            for (uint32_t spins = 0;; ++spins)
            {
                int32_t current = state.load(std::memory_order_relaxed);
                if ((current & (STELLAR_NATIVE_LIST_WRITER | STELLAR_NATIVE_LIST_WAITING)) == 0
                    && state.compare_exchange_weak(current, current + 1, std::memory_order_acquire, std::memory_order_relaxed))
                    return;

                Pause(spins);
            }
        }

        static void ExitRead(Link<StellarNativeList> list) noexcept
        {
            std::atomic_ref<int32_t>(list.lock).fetch_sub(1, std::memory_order_release);
        }

        static void EnterWrite(Link<StellarNativeList> list) noexcept
        {
            std::atomic_ref<int32_t> state(list.lock);
            for (uint32_t spins = 0;; ++spins)
            {
                int32_t current = state.load(std::memory_order_relaxed);
                if ((current & ~STELLAR_NATIVE_LIST_WAITING) == 0)
                {
                    if (state.compare_exchange_weak(current, STELLAR_NATIVE_LIST_WRITER, std::memory_order_acquire,
                                                    std::memory_order_relaxed))
                        return;
                }
                else if ((current & STELLAR_NATIVE_LIST_WAITING) == 0)
                {
                    state.compare_exchange_weak(current, current | STELLAR_NATIVE_LIST_WAITING, std::memory_order_relaxed);
                }

                Pause(spins);
            }
        }

        static void ExitWrite(Link<StellarNativeList> list) noexcept
        {
            std::atomic_ref<int32_t>(list.lock).fetch_and(~STELLAR_NATIVE_LIST_WRITER, std::memory_order_release);
        }
    };

    /// @brief Holds the lock of a list for reading while it lives.
    class NativeListReadScope final
    {
        Ptr<StellarNativeList> _list;

    public:
        explicit NativeListReadScope(Link<StellarNativeList> list) noexcept :
            _list(list) { NativeListLock::EnterRead(list); }

        ~NativeListReadScope() { NativeListLock::ExitRead(*_list); }

        NativeListReadScope(const NativeListReadScope&) = delete;
        NativeListReadScope& operator=(const NativeListReadScope&) = delete;
    };

    /// @brief Holds the lock of a list for writing while it lives.
    class NativeListWriteScope final
    {
        Ptr<StellarNativeList> _list;

    public:
        explicit NativeListWriteScope(Link<StellarNativeList> list) noexcept :
            _list(list) { NativeListLock::EnterWrite(list); }

        ~NativeListWriteScope() { NativeListLock::ExitWrite(*_list); }

        NativeListWriteScope(const NativeListWriteScope&) = delete;
        NativeListWriteScope& operator=(const NativeListWriteScope&) = delete;
    };

    /// @brief Works on the elements of a native list: a view over a header someone owns, C# or C++.
    ///
    /// Takes no lock: on a concurrent list, hold a NativeListReadScope or a NativeListWriteScope around it. A change
    /// that grows the buffer goes through the allocator of the header, so a list made in C# grows in C++ and back.
    template <typename T> requires std::is_trivially_copyable_v<T>
    class NativeListView final
    {
        Ptr<StellarNativeList> _list;
        Ptr<const StellarMemoryApi> _api;

    public:
        /// @param list The header; its element size must be sizeof(T) (IsOf).
        /// @param api The functions of the allocators, for growing.
        NativeListView(Link<StellarNativeList> list, Link<const StellarMemoryApi> api) noexcept :
            _list(list), _api(api) {}

        /// @brief Gets whether a header holds elements of type T.
        [[nodiscard]] static bool IsOf(Link<const StellarNativeList> list) noexcept
        {
            return list.elementSize == static_cast<int32_t>(sizeof(T));
        }

        [[nodiscard]] int32_t Count() const noexcept { return _list->count; }

        [[nodiscard]] int32_t Capacity() const noexcept { return _list->capacity; }

        /// @brief The elements, valid until the next change that grows the list.
        [[nodiscard]] std::span<T> Items() const noexcept
        {
            return {static_cast<T*>(_list->data), static_cast<size_t>(_list->count)};
        }

        /// @brief Makes room for at least @p capacity elements.
        /// @return false when out of memory: the list is unchanged.
        [[nodiscard]] bool Reserve(int32_t capacity) noexcept
        {
            if (capacity <= _list->capacity)
                return true;

            const auto size = static_cast<size_t>(_list->elementSize);
            void* data = _api->reallocate(_list->allocator, _list->data, size * static_cast<size_t>(_list->capacity),
                                          size * static_cast<size_t>(capacity), _list->alignment);
            if (!data)
                return false;

            _list->data = data;
            _list->capacity = capacity;
            ++_list->version;
            return true;
        }

        /// @brief Adds an element at the end, doubling the buffer when full.
        /// @return false when out of memory: the list is unchanged.
        [[nodiscard]] bool Add(const T& item) noexcept
        {
            if (_list->count == _list->capacity && !Reserve(_list->capacity < 4 ? 4 : _list->capacity * 2))
                return false;

            std::memcpy(static_cast<T*>(_list->data) + _list->count, &item, sizeof(T));
            ++_list->count;
            return true;
        }

        /// @brief Removes an element by moving the last one into its place: no shift, the order changes.
        void RemoveAtSwapBack(int32_t index) noexcept
        {
            const auto items = static_cast<T*>(_list->data);
            const int32_t last = _list->count - 1;
            if (index != last)
                std::memcpy(items + index, items + last, sizeof(T));

            --_list->count;
            ++_list->version;
        }

        /// @brief Removes every element, keeping the buffer.
        void Clear() noexcept
        {
            _list->count = 0;
            ++_list->version;
        }
    };

    /// @brief A native list owned by C++: made and freed with this object.
    template <typename T> requires std::is_trivially_copyable_v<T>
    class NativeList final
    {
        StellarNativeList _list{};
        Ptr<const StellarMemoryApi> _api;

    public:
        /// @param allocator The allocator of the buffer; the list holds a reference to it.
        /// @param api The functions of the allocators.
        NativeList(Ptr<StellarAllocator> allocator, Link<const StellarMemoryApi> api) noexcept :
            _api(api)
        {
            _list.elementSize = static_cast<int32_t>(sizeof(T));
            _list.alignment = static_cast<uint32_t>(alignof(T) < 16 ? 16 : alignof(T));
            _list.allocator = allocator.get();
            _api->acquire(_list.allocator);
        }

        ~NativeList()
        {
            _api->free(_list.allocator, _list.data, static_cast<size_t>(_list.elementSize) * static_cast<size_t>(_list.capacity));
            _api->release(_list.allocator);
        }

        NativeList(const NativeList&) = delete;
        NativeList& operator=(const NativeList&) = delete;

        /// @brief The header, for whoever works on the list: C# through a pointer, another native module.
        [[nodiscard]] Link<StellarNativeList> Header() noexcept { return _list; }

        [[nodiscard]] NativeListView<T> View() noexcept { return NativeListView<T>(_list, *_api); }
    };
}
