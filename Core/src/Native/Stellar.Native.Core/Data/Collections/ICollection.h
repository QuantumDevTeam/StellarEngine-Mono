// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Collections/ICollection.h
/// @brief The contract of the concurrent engine collections.
#pragma once

#include <concepts>
#include <cstddef>
#include <functional>
#include <vector>
#include <optional>
#include <type_traits>

namespace Stellar::Native::Core::Data::Collections
{
    /// @brief Satisfied by keys usable in hash maps.
    template <typename Key>
    concept CHashableKey =
            requires(Key k)
            {
                { std::hash<Key>{}(k) } -> std::convertible_to<std::size_t>;
                { k == k } -> std::convertible_to<bool>;
            };

    /// @brief Satisfied by keys with an order.
    template <typename Key>
    concept CSortableKey =
            requires(Key k)
            {
                { k < k } -> std::convertible_to<bool>;
                { k == k } -> std::convertible_to<bool>;
            };

    namespace INTERNAL
    {
        template <typename T>
        struct IsOptional : std::false_type
        {
            using value_type = T;
        };

        template <typename T>
        struct IsOptional<std::optional<T>> : std::true_type
        {
            using value_type = T;
        };
    }

    /// @brief Satisfied by a callable that maps a pair to @p T or to an optional @p T.
    template <typename F, typename T, typename TKey, typename TValue>
    concept CIterationInvoking =
            std::invocable<F, Link<const TKey>, Link<const TValue>> &&
            std::same_as<typename INTERNAL::IsOptional<std::invoke_result_t<F, Link<const TKey>, Link<const TValue>>>::value_type, T>;

    /// @brief Satisfied by a predicate over a pair.
    template <typename F, typename TKey, typename TValue>
    concept CPredicate = std::predicate<F, Link<const TKey>, Link<const TValue>>;

    /// @brief The contract of the concurrent engine collections.
    ///
    /// Enumeration returns copies taken under a lock: no lock is ever held while the caller's code runs, and the
    /// result may safely outlive any concurrent modification.
    template <CHashableKey TKey, typename TValue>
    class ICollection
    {
    public:
        ICollection() = default;
        virtual ~ICollection() = default;

        ICollection(const ICollection&) = delete;
        ICollection& operator=(const ICollection&) = delete;

        /// @brief Adds a pair. @return False if the key exists.
        virtual bool TryAdd(Link<const TKey> key, Link<const TValue> value) = 0;

        /// @brief Gets a value by its key.
        [[nodiscard]] virtual std::optional<TValue> TryGet(Link<const TKey> key) const = 0;

        /// @brief Removes a pair. @return False if the key does not exist.
        virtual bool TryRemove(Link<const TKey> key, Link<TValue> outValue) = 0;

        /// @brief Checks for a key.
        [[nodiscard]] virtual bool Contains(Link<const TKey> key) const = 0;

        /// @brief Gets the number of pairs.
        [[nodiscard]] virtual std::size_t size() const = 0;

        /// @brief Gets a snapshot of the keys.
        [[nodiscard]] virtual std::vector<TKey> Keys() const = 0;

        /// @brief Gets a snapshot of the values.
        [[nodiscard]] virtual std::vector<TValue> Values() const = 0;

        /// @brief Removes everything.
        virtual void Clear() = 0;
    };

    /// @brief The default number of independently locked segments of a map.
    inline constexpr std::size_t DefaultNumSegments = 16;
}
