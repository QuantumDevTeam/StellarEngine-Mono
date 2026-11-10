// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Registry/ContainerRegistry.h
/// @brief Containers of identifiers: the native side of DataContainer and Roster.
#pragma once

#include <cstddef>
#include <memory>
#include <vector>

#include "Identifier.h"

namespace Stellar::Native::Core::Data::Registry
{
    /// @brief The containers of one engine (CoreContext).
    ///
    /// A container is a set of identifiers addressed by an identifier of its own: which objects belong to it is
    /// known here, on the native side, so native systems can walk a container without asking managed code. The
    /// objects themselves live wherever their owner keeps them.
    ///
    /// A container may have branches, other containers nested in it: this is the tree of a Roster.
    /// A sealed container is read-only: this is a constant table.
    ///
    /// Operations on a container that does not exist throw Failures::NativeException with STELLAR_RESULT_NOT_FOUND.
    class STELLAR_CORE_API ContainerRegistry final
    {
        struct Impl;
        UniquePtr<Impl> _impl;

    public:
        /// @brief Creates an empty registry. A CoreContext owns one; tests may make their own.
        ContainerRegistry();
        ~ContainerRegistry();

        ContainerRegistry(const ContainerRegistry&) = delete;
        ContainerRegistry& operator=(const ContainerRegistry&) = delete;

        /// @brief Creates an empty container. @return False if it exists already or the identifier is null.
        bool Create(Link<const Identifier> container);

        /// @brief Destroys a container. Its branches survive. @return False if it does not exist.
        bool Destroy(Link<const Identifier> container);

        /// @brief Checks whether a container exists.
        [[nodiscard]] bool Exists(Link<const Identifier> container) const;

        /// @brief Makes a container read-only, for good.
        void Seal(Link<const Identifier> container);

        /// @brief Checks whether a container is read-only.
        [[nodiscard]] bool IsSealed(Link<const Identifier> container) const;

        /// @brief Adds an item. @return False if it is already there or the container is sealed.
        bool Add(Link<const Identifier> container, Link<const Identifier> item);

        /// @brief Removes an item. @return False if it is not there or the container is sealed.
        bool Remove(Link<const Identifier> container, Link<const Identifier> item);

        /// @brief Checks for an item.
        [[nodiscard]] bool Contains(Link<const Identifier> container, Link<const Identifier> item) const;

        /// @brief Gets the number of items.
        [[nodiscard]] std::size_t Count(Link<const Identifier> container) const;

        /// @brief Gets a snapshot of the items.
        [[nodiscard]] std::vector<Identifier> Items(Link<const Identifier> container) const;

        /// @brief Removes every item. @return False if the container is sealed.
        bool Clear(Link<const Identifier> container);

        /// @brief Nests a container. The branch must exist. @return False if it is nested already or is the container itself.
        bool AddBranch(Link<const Identifier> container, Link<const Identifier> branch);

        /// @brief Removes a nested container. @return False if it is not nested.
        bool RemoveBranch(Link<const Identifier> container, Link<const Identifier> branch);

        /// @brief Checks for a nested container.
        [[nodiscard]] bool ContainsBranch(Link<const Identifier> container, Link<const Identifier> branch) const;

        /// @brief Gets a snapshot of the nested containers.
        [[nodiscard]] std::vector<Identifier> Branches(Link<const Identifier> container) const;

        /// @brief Gets the number of containers.
        [[nodiscard]] std::size_t size() const;
    };
}
