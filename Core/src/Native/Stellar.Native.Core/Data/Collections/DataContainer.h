// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Collections/DataContainer.h
/// @brief Identifiable containers: the native side of `Stellar.Core.Data.Collections`.
#pragma once

#include <initializer_list>
#include <utility>

#include "Identifier.h"
#include "Mixins/Identifiable.h"
#include "Data/Collections/ConcurrentUnorderedMap.h"

namespace Stellar::Native::Core::Data::Collections
{
    /// @brief A writable container with an identity of its own.
    template <typename TValue>
    class DataContainer final : public ConcurrentUnorderedIdentifierMap<TValue>, public Identifiable
    {
    public:
        /// @brief Creates an empty container with a new identifier.
        DataContainer() :
            Identifiable(Identifier::Create()) {}
    };

    /// @brief A container whose content is fixed at creation.
    template <typename TValue>
    class ConstantDataContainer final : public ConcurrentUnorderedIdentifierMap<TValue>, public Identifiable
    {
        using Base = ConcurrentUnorderedIdentifierMap<TValue>;

    public:
        /// @brief Creates a container with a new identifier.
        /// @param content The content.
        ConstantDataContainer(std::initializer_list<std::pair<Identifier, TValue>> content) :
            Identifiable(Identifier::Create())
        {
            for (const auto& [key, value] : content)
                Base::TryAdd(key, value);
        }

        bool TryAdd(Link<const Identifier>, Link<const TValue>) override { return false; }

        bool TryRemove(Link<const Identifier>, Link<TValue>) override { return false; }
    };
}
