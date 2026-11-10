// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Modules/SharedLibrary.h
/// @brief The only place of the core that knows how the system loads libraries. Internal to the core.
#pragma once

#include <string>
#include <string_view>

namespace Stellar::Native::Core::Modules
{
    /// @brief An owned shared library.
    class SharedLibrary final
    {
        Ptr<void> _handle;

        void Close() noexcept;

    public:
        SharedLibrary() noexcept = default;
        ~SharedLibrary();

        SharedLibrary(const SharedLibrary&) = delete;
        SharedLibrary& operator=(const SharedLibrary&) = delete;

        SharedLibrary(SharedLibrary&& other) noexcept;
        SharedLibrary& operator=(SharedLibrary&& other) noexcept;

        /// @brief Loads a library.
        /// @param path The UTF-8 path.
        /// @throws Failures::NativeException if the library cannot be loaded.
        [[nodiscard]] static SharedLibrary Open(std::string_view path);

        /// @brief Finds an exported symbol.
        /// @return The address, or null if there is no such symbol.
        [[nodiscard]] Ptr<void> FindSymbol(Link<const std::string> name) const noexcept;
    };
}
