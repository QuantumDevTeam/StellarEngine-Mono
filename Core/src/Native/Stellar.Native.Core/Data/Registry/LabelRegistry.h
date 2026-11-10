// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Data/Registry/LabelRegistry.h
/// @brief The registry of labels.
#pragma once

#include <cstddef>
#include <memory>
#include <optional>
#include <string_view>

#include "Identifier.h"
#include "Label.h"

namespace Stellar::Native::Core::Data::Registry
{
    /// @brief The registry of labels of one engine (CoreContext): lookup by identifier and by name.
    ///
    /// The registry owns a copy of every name. The labels it returns stay valid until they are unregistered.
    class STELLAR_CORE_API LabelRegistry final
    {
        struct Impl;
        UniquePtr<Impl> _impl;

    public:
        /// @brief Creates an empty registry. A CoreContext owns one; tests may make their own.
        LabelRegistry();
        ~LabelRegistry();

        LabelRegistry(const LabelRegistry&) = delete;
        LabelRegistry& operator=(const LabelRegistry&) = delete;

        /// @brief Adds a label. The name is copied.
        /// @param name The name.
        /// @param id The identifier to bind the label to.
        /// @return The registered label, or nothing if the name or the identifier is taken.
        std::optional<Label> Register(std::string_view name, Link<const Identifier> id);

        /// @brief Finds a label by its identifier.
        [[nodiscard]] std::optional<Label> Get(Link<const Identifier> id) const;

        /// @brief Finds a label by its name.
        [[nodiscard]] std::optional<Label> Get(std::string_view name) const;

        /// @brief Removes a label. @return False if it is not registered.
        bool Unregister(Link<const Identifier> id);

        /// @brief Removes a label. @return False if it is not registered.
        bool Unregister(std::string_view name);

        /// @brief Checks for a label.
        [[nodiscard]] bool Contains(Link<const Identifier> id) const;

        /// @brief Gets the number of labels.
        [[nodiscard]] std::size_t size() const;
    };
}
