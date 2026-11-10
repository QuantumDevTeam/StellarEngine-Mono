// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Collections/ConcurrentUnorderedMap.h
/// @brief A hash map split into independently locked segments.
#pragma once

#include <algorithm>
#include <memory>
#include <mutex>
#include <shared_mutex>
#include <unordered_map>
#include <utility>
#include <vector>

#include "Identifier.h"
#include "Data/Collections/ICollection.h"

namespace Stellar::Native::Core::Data::Collections
{
    /// @brief A hash map split into independently locked segments.
    ///
    /// Threads that touch different segments never wait for each other.
    template <CHashableKey TKey, typename TValue>
    class ConcurrentUnorderedMap : public ICollection<TKey, TValue>
    {
    public:
        /// @brief Creates a map.
        /// @param numSegments The number of segments, at least one.
        explicit ConcurrentUnorderedMap(std::size_t numSegments = DefaultNumSegments)
        {
            const std::size_t segmentCount = std::max<std::size_t>(1, numSegments);

            _segments.reserve(segmentCount);
            for (std::size_t i = 0; i < segmentCount; ++i)
                _segments.push_back(std::make_unique<Segment>());
        }

        bool TryAdd(Link<const TKey> key, Link<const TValue> value) override
        {
            auto& segment = SegmentOf(key);
            std::unique_lock lock(segment.mutex);
            return segment.data.try_emplace(key, value).second;
        }

        [[nodiscard]] std::optional<TValue> TryGet(Link<const TKey> key) const final
        {
            auto& segment = SegmentOf(key);
            std::shared_lock lock(segment.mutex);

            const auto it = segment.data.find(key);
            if (it == segment.data.end())
                return std::nullopt;

            return it->second;
        }

        bool TryRemove(Link<const TKey> key, Link<TValue> outValue) override
        {
            auto& segment = SegmentOf(key);
            std::unique_lock lock(segment.mutex);

            const auto it = segment.data.find(key);
            if (it == segment.data.end())
                return false;

            outValue = std::move(it->second);
            segment.data.erase(it);
            return true;
        }

        [[nodiscard]] bool Contains(Link<const TKey> key) const final
        {
            auto& segment = SegmentOf(key);
            std::shared_lock lock(segment.mutex);
            return segment.data.contains(key);
        }

        [[nodiscard]] std::size_t size() const final
        {
            std::size_t total = 0;

            for (const auto& segment : _segments)
            {
                std::shared_lock lock(segment->mutex);
                total += segment->data.size();
            }

            return total;
        }

        [[nodiscard]] std::vector<TKey> Keys() const final
        {
            std::vector<TKey> keys;

            for (const auto& segment : _segments)
            {
                std::shared_lock lock(segment->mutex);

                for (const auto& [key, value] : segment->data)
                    keys.push_back(key);
            }

            return keys;
        }

        [[nodiscard]] std::vector<TValue> Values() const final
        {
            std::vector<TValue> values;

            for (const auto& segment : _segments)
            {
                std::shared_lock lock(segment->mutex);

                for (const auto& [key, value] : segment->data)
                    values.push_back(value);
            }

            return values;
        }

        /// @brief Maps the pairs, each segment under its read lock.
        /// @param action Maps a pair to @p TReturn. Returning an empty optional skips the pair.
        /// @return The results.
        template <typename TReturn, CIterationInvoking<TReturn, TKey, TValue> F>
        [[nodiscard]] std::vector<TReturn> ForEachReadOnly(F action) const
        {
            using Result = std::invoke_result_t<F, Link<const TKey>, Link<const TValue>>;

            std::vector<TReturn> results;

            for (const auto& segment : _segments)
            {
                std::shared_lock lock(segment->mutex);

                for (const auto& [key, value] : segment->data)
                {
                    if constexpr (INTERNAL::IsOptional<Result>::value)
                    {
                        if (auto result = action(key, value); result.has_value())
                            results.push_back(std::move(*result));
                    }
                    else
                    {
                        results.push_back(action(key, value));
                    }
                }
            }

            return results;
        }

        /// @brief Removes the pairs that satisfy a predicate.
        /// @return The removed pairs.
        template <CPredicate<TKey, TValue> Pred>
        std::vector<std::pair<TKey, TValue>> ForEachPredicateRemove(Pred predicate)
        {
            std::vector<std::pair<TKey, TValue>> removed;

            for (auto& segment : _segments)
            {
                std::unique_lock lock(segment->mutex);

                for (auto it = segment->data.begin(); it != segment->data.end();)
                {
                    if (predicate(it->first, it->second))
                    {
                        removed.emplace_back(it->first, std::move(it->second));
                        it = segment->data.erase(it);
                    }
                    else
                    {
                        ++it;
                    }
                }
            }

            return removed;
        }

        void Clear() final
        {
            for (auto& segment : _segments)
            {
                std::unique_lock lock(segment->mutex);
                segment->data.clear();
            }
        }

    private:
        struct Segment
        {
            mutable std::shared_mutex mutex;
            std::unordered_map<TKey, TValue> data;
        };

        [[nodiscard]] Link<Segment> SegmentOf(Link<const TKey> key) const noexcept
        {
            return *_segments[std::hash<TKey>{}(key) % _segments.size()];
        }

        std::vector<std::unique_ptr<Segment>> _segments;
    };

    /// @brief A concurrent map keyed by identifier.
    template <typename TValue>
    using ConcurrentUnorderedIdentifierMap = ConcurrentUnorderedMap<Identifier, TValue>;
}
