// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Collections/HandlePool.h
/// @brief Storage of objects addressed by generational handles.
#pragma once

#include <cstdint>
#include <deque>
#include <mutex>
#include <optional>
#include <shared_mutex>
#include <utility>
#include <vector>

namespace Stellar::Native::Core::Data::Collections
{
    /// @brief Storage of objects addressed by generational handles.
    ///
    /// This is how a native object is exposed to the outside: C# keeps a StellarHandle, never a pointer. Destroying
    /// an object advances the generation of its slot, so a stale handle is rejected even when the slot is reused.
    /// Freed slots are reused, the storage does not grow with churn.
    template <typename T>
    class HandlePool
    {
    public:
        HandlePool() = default;

        HandlePool(const HandlePool&) = delete;
        HandlePool& operator=(const HandlePool&) = delete;

        /// @brief Constructs an object.
        /// @return Its handle.
        template <typename... TArgs>
        [[nodiscard]] StellarHandle Create(TArgs&&... args)
        {
            std::unique_lock lock(_mutex);

            uint32_t index;

            if (_free.empty())
            {
                index = static_cast<uint32_t>(_slots.size());
                _slots.emplace_back();
            }
            else
            {
                index = _free.back();
                _free.pop_back();
            }

            Slot& slot = _slots[index];
            slot.value.emplace(std::forward<TArgs>(args)...);

            return StellarHandle{index, slot.generation};
        }

        /// @brief Destroys an object.
        /// @return False if the handle is stale.
        bool Destroy(StellarHandle handle)
        {
            std::unique_lock lock(_mutex);

            Ptr<Slot> slot = Find(handle);
            if (!slot)
                return false;

            Release(handle, *slot);
            return true;
        }

        /// @brief Takes an object out of the pool.
        /// @return The object, or nothing if the handle is stale.
        [[nodiscard]] std::optional<T> Take(StellarHandle handle)
        {
            std::unique_lock lock(_mutex);

            Ptr<Slot> slot = Find(handle);
            if (!slot)
                return std::nullopt;

            std::optional<T> result(std::move(slot->value));
            Release(handle, *slot);
            return result;
        }

        /// @brief Checks whether a handle refers to a live object.
        [[nodiscard]] bool IsValid(StellarHandle handle) const
        {
            std::shared_lock lock(_mutex);
            return Find(handle) != nullptr;
        }

        /// @brief Calls a function with an object, under a read lock.
        /// @param function Receives the object. It must not call back into this pool.
        /// @return False if the handle is stale.
        template <typename F>
        bool With(StellarHandle handle, F&& function) const
        {
            std::shared_lock lock(_mutex);

            Ptr<const Slot> slot = Find(handle);
            if (!slot)
                return false;

            std::forward<F>(function)(*slot->value);
            return true;
        }

        /// @brief Gets the number of live objects.
        [[nodiscard]] std::size_t size() const
        {
            std::shared_lock lock(_mutex);
            return _slots.size() - _free.size();
        }

    private:
        struct Slot
        {
            std::optional<T> value;
            uint32_t generation = 1;
        };

        [[nodiscard]] Ptr<Slot> Find(StellarHandle handle)
        {
            return const_cast<Ptr<Slot>::element_type*>(std::as_const(*this).Find(handle).get());
        }

        [[nodiscard]] Ptr<const Slot> Find(StellarHandle handle) const
        {
            if (handle.generation == 0 || handle.index >= _slots.size())
                return nullptr;

            Link<const Slot> slot = _slots[handle.index];

            return slot.generation == handle.generation && slot.value.has_value() ? Ptr<const Slot>(slot) : nullptr;
        }

        void Release(StellarHandle handle, Link<Slot> slot)
        {
            slot.value.reset();

            // Zero marks an invalid handle, so it is skipped when the counter wraps around
            if (++slot.generation == 0)
                slot.generation = 1;

            _free.push_back(handle.index);
        }

        mutable std::shared_mutex _mutex;
        std::deque<Slot> _slots;
        std::vector<uint32_t> _free;
    };
}
