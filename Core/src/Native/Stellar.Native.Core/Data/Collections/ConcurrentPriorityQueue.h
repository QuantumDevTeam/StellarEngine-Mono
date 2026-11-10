// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Collections/ConcurrentPriorityQueue.h
/// @brief A blocking priority queue.
#pragma once

#include <condition_variable>
#include <functional>
#include <mutex>
#include <optional>
#include <queue>
#include <stop_token>
#include <utility>
#include <vector>

namespace Stellar::Native::Core::Data::Collections
{
    /// @brief A blocking priority queue.
    ///
    /// One heap under one lock: priority is a global order, and any split into segments would lose it. Elements
    /// only need to be movable and comparable.
    template <typename T, typename Compare = std::less<T>>
    class ConcurrentPriorityQueue
    {
    public:
        ConcurrentPriorityQueue() = default;

        ConcurrentPriorityQueue(const ConcurrentPriorityQueue&) = delete;
        ConcurrentPriorityQueue& operator=(const ConcurrentPriorityQueue&) = delete;

        /// @brief Adds an element and wakes one waiting thread.
        void Push(T value)
        {
            {
                std::lock_guard lock(_mutex);
                _heap.push(std::move(value));
            }

            _available.notify_one();
        }

        /// @brief Takes the element of the highest priority, if there is one.
        [[nodiscard]] std::optional<T> TryPop()
        {
            std::lock_guard lock(_mutex);
            return PopLocked();
        }

        /// @brief Waits for an element.
        /// @param stop Cancels the wait.
        /// @return The element, or nothing if the wait was cancelled.
        [[nodiscard]] std::optional<T> PopWait(std::stop_token stop)
        {
            std::unique_lock lock(_mutex);

            if (!_available.wait(lock, stop, [this] { return !_heap.empty(); }))
                return std::nullopt;

            return PopLocked();
        }

        /// @brief Gets the number of waiting elements.
        [[nodiscard]] std::size_t size() const
        {
            std::lock_guard lock(_mutex);
            return _heap.size();
        }

        /// @brief Removes everything.
        void Clear()
        {
            std::lock_guard lock(_mutex);
            _heap = {};
        }

    private:
        [[nodiscard]] std::optional<T> PopLocked()
        {
            if (_heap.empty())
                return std::nullopt;

            // std::priority_queue only exposes a const top: moving it out is safe, the heap drops it right away
            std::optional<T> result(std::move(const_cast<T&>(_heap.top())));
            _heap.pop();
            return result;
        }

        mutable std::mutex _mutex;
        std::condition_variable_any _available;
        std::priority_queue<T, std::vector<T>, Compare> _heap;
    };
}
