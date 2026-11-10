// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Collections/ConcurrentVector.h
/// @brief A thread-safe vector.
#pragma once

#include <algorithm>
#include <cstddef>
#include <mutex>
#include <shared_mutex>
#include <utility>
#include <vector>

namespace Stellar::Native::Core::Data::Collections
{
    /// @brief A thread-safe vector.
    template <typename T>
    class ConcurrentVector
    {
    public:
        ConcurrentVector() = default;

        ConcurrentVector(const ConcurrentVector&) = delete;
        ConcurrentVector& operator=(const ConcurrentVector&) = delete;

        /// @brief Appends an element.
        void PushBack(T value)
        {
            std::unique_lock lock(_mutex);
            _data.push_back(std::move(value));
        }

        /// @brief Copies an element. @return False if the index is out of range.
        bool TryGet(std::size_t index, Link<T> outValue) const
        {
            std::shared_lock lock(_mutex);

            if (index >= _data.size())
                return false;

            outValue = _data[index];
            return true;
        }

        /// @brief Removes an element. @return False if the index is out of range.
        bool TryRemove(std::size_t index, Link<T> outValue)
        {
            std::unique_lock lock(_mutex);

            if (index >= _data.size())
                return false;

            outValue = std::move(_data[index]);
            _data.erase(_data.begin() + static_cast<std::ptrdiff_t>(index));
            return true;
        }

        /// @brief Sorts the elements.
        template <typename Compare = std::less<T>>
        void Sort(Compare comparator = {})
        {
            std::unique_lock lock(_mutex);
            std::sort(_data.begin(), _data.end(), std::move(comparator));
        }

        /// @brief Gets the number of elements.
        [[nodiscard]] std::size_t size() const
        {
            std::shared_lock lock(_mutex);
            return _data.size();
        }

        /// @brief Removes everything.
        void Clear()
        {
            std::unique_lock lock(_mutex);
            _data.clear();
        }

    private:
        mutable std::shared_mutex _mutex;
        std::vector<T> _data;
    };
}
