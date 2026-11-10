// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Collections/DoubleBufferedSnapshotMap.h
/// @brief A map for frame-based reading: a stable snapshot for readers, a working copy for writers.
#pragma once

#include <atomic>
#include <memory>
#include <mutex>
#include <shared_mutex>
#include <unordered_map>
#include <vector>

#include "Identifier.h"
#include "Data/Collections/ICollection.h"

namespace Stellar::Native::Core::Data::Collections
{
    /// @brief A map for frame-based reading: a stable snapshot for readers, a working copy for writers.
    ///
    /// Readers work with an immutable snapshot and take no lock: a loop over a frame sees one consistent state.
    /// Writers change the working copy, which becomes visible on TakeSnapshot.
    ///
    /// Passing `immediate = true` additionally publishes a change as "hot": it is seen right away by the readers
    /// that also ask with `immediate = true`, before the next snapshot.
    template <CHashableKey TKey, typename TValue>
    class DoubleBufferedSnapshotMap : public ICollection<TKey, TValue>
    {
    public:
        /// @brief An immutable state of the map.
        struct DataSnapshot
        {
            std::unordered_map<TKey, TValue> map;
            uint64_t version = 0;
        };

        DoubleBufferedSnapshotMap() :
            _currentSnapshot(std::make_shared<const DataSnapshot>()) {}

        /// @brief Gets the current snapshot. Lock-free.
        [[nodiscard]] std::shared_ptr<const DataSnapshot> GetSnapshot() const
        {
            return _currentSnapshot.load(std::memory_order_acquire);
        }

        /// @brief Publishes the working copy as the new snapshot and forgets the hot changes.
        /// @return The new snapshot.
        std::shared_ptr<const DataSnapshot> TakeSnapshot()
        {
            std::unique_lock lock(_writeMutex);
            return TakeSnapshotLocked();
        }

        /// @brief Adds a pair to the working copy. @return False if the key exists.
        bool TryAdd(Link<const TKey> key, Link<const TValue> value, bool immediate)
        {
            std::unique_lock lock(_writeMutex);

            if (!_writeMap.try_emplace(key, value).second)
                return false;

            if (immediate)
                SetHot(key, value);

            return true;
        }

        /// @brief Gets a value. @param immediate Whether to see hot changes.
        [[nodiscard]] std::optional<TValue> TryGet(Link<const TKey> key, bool immediate) const
        {
            if (immediate)
            {
                std::shared_lock lock(_hotMutex);

                if (const auto it = _hotItems.find(key); it != _hotItems.end())
                    return it->second;
            }

            const auto snapshot = GetSnapshot();
            const auto it = snapshot->map.find(key);

            if (it == snapshot->map.end())
                return std::nullopt;

            return it->second;
        }

        /// @brief Removes a pair from the working copy. @return False if the key does not exist.
        bool TryRemove(Link<const TKey> key, Link<TValue> outValue, bool immediate)
        {
            std::unique_lock lock(_writeMutex);

            const auto it = _writeMap.find(key);
            if (it == _writeMap.end())
                return false;

            outValue = std::move(it->second);
            _writeMap.erase(it);

            if (immediate)
                SetHot(key, std::nullopt);

            return true;
        }

        /// @brief Checks for a key. @param immediate Whether to see hot changes.
        [[nodiscard]] bool Contains(Link<const TKey> key, bool immediate) const
        {
            return TryGet(key, immediate).has_value();
        }

        /// @brief Gets the number of pairs. @param immediate Whether to count hot changes.
        [[nodiscard]] std::size_t size(bool immediate) const
        {
            const auto snapshot = GetSnapshot();
            std::size_t total = snapshot->map.size();

            if (!immediate)
                return total;

            std::shared_lock lock(_hotMutex);

            for (const auto& [key, value] : _hotItems)
            {
                const bool inSnapshot = snapshot->map.contains(key);

                if (value.has_value() && !inSnapshot)
                    ++total;
                else if (!value.has_value() && inSnapshot)
                    --total;
            }

            return total;
        }

        /// @brief Gets a snapshot of the keys. @param immediate Whether to see hot changes.
        [[nodiscard]] std::vector<TKey> Keys(bool immediate) const
        {
            std::vector<TKey> keys;

            for (const auto& [key, value] : Merged(immediate))
                keys.push_back(key);

            return keys;
        }

        /// @brief Gets a snapshot of the values. @param immediate Whether to see hot changes.
        [[nodiscard]] std::vector<TValue> Values(bool immediate) const
        {
            std::vector<TValue> values;

            for (const auto& [key, value] : Merged(immediate))
                values.push_back(value);

            return values;
        }

        bool TryAdd(Link<const TKey> key, Link<const TValue> value) final { return TryAdd(key, value, false); }

        [[nodiscard]] std::optional<TValue> TryGet(Link<const TKey> key) const final { return TryGet(key, false); }

        bool TryRemove(Link<const TKey> key, Link<TValue> outValue) final { return TryRemove(key, outValue, false); }

        [[nodiscard]] bool Contains(Link<const TKey> key) const final { return Contains(key, false); }

        [[nodiscard]] std::size_t size() const final { return size(false); }

        [[nodiscard]] std::vector<TKey> Keys() const final { return Keys(false); }

        [[nodiscard]] std::vector<TValue> Values() const final { return Values(false); }

        /// @brief Removes everything and publishes the empty state.
        void Clear() final
        {
            std::unique_lock lock(_writeMutex);
            _writeMap.clear();
            TakeSnapshotLocked();
        }

    private:
        void SetHot(Link<const TKey> key, std::optional<TValue> value)
        {
            std::unique_lock lock(_hotMutex);
            _hotItems.insert_or_assign(key, std::move(value));
        }

        std::shared_ptr<const DataSnapshot> TakeSnapshotLocked()
        {
            auto next = std::make_shared<DataSnapshot>();

            next->map = _writeMap;
            next->version = GetSnapshot()->version + 1;

            std::shared_ptr<const DataSnapshot> published = std::move(next);
            _currentSnapshot.store(published, std::memory_order_release);

            std::unique_lock lock(_hotMutex);
            _hotItems.clear();

            return published;
        }

        [[nodiscard]] std::unordered_map<TKey, TValue> Merged(bool immediate) const
        {
            auto merged = GetSnapshot()->map;

            if (!immediate)
                return merged;

            std::shared_lock lock(_hotMutex);

            for (const auto& [key, value] : _hotItems)
            {
                if (value.has_value())
                    merged.insert_or_assign(key, *value);
                else
                    merged.erase(key);
            }

            return merged;
        }

        std::atomic<std::shared_ptr<const DataSnapshot>> _currentSnapshot;

        // Lock order: _writeMutex, then _hotMutex
        mutable std::shared_mutex _writeMutex;
        std::unordered_map<TKey, TValue> _writeMap;

        mutable std::shared_mutex _hotMutex;
        std::unordered_map<TKey, std::optional<TValue>> _hotItems;
    };

    /// @brief A double-buffered map keyed by identifier.
    template <typename TValue>
    using DoubleBufferedSnapshotIdentifierMap = DoubleBufferedSnapshotMap<Identifier, TValue>;
}
