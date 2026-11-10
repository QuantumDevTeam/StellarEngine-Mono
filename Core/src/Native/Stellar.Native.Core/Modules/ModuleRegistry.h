// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Modules/ModuleRegistry.h
/// @brief Loading of native modules and checking of their contracts.
#pragma once

#include <cstddef>
#include <memory>
#include <optional>
#include <string>
#include <string_view>

namespace Stellar::Native::Core::Modules
{
    /// @brief Checks whether a module built against @p required can run on this engine.
    [[nodiscard]] constexpr bool IsContractSatisfied(StellarContractVersion provided, StellarContractVersion required) noexcept
    {
        return provided.major == required.major && provided.minor >= required.minor;
    }

    /// @brief The native modules one engine (CoreContext) loaded.
    ///
    /// A module is validated before any of its code beyond StellarModule_GetInfo runs: an incompatible module is
    /// reported as STELLAR_RESULT_VERSION_MISMATCH instead of crashing later.
    class STELLAR_CORE_API ModuleRegistry final
    {
        struct Impl;
        UniquePtr<Impl> _impl;

    public:
        /// @brief Creates an empty registry. A CoreContext owns one; tests may make their own.
        ModuleRegistry();
        ~ModuleRegistry();

        ModuleRegistry(const ModuleRegistry&) = delete;
        ModuleRegistry& operator=(const ModuleRegistry&) = delete;

        /// @brief Loads a module from a shared library and initializes it.
        /// @param path The UTF-8 path of the library.
        /// @return The handle of the module.
        /// @throws Failures::NativeException if the library is not a valid module or its initialization fails.
        [[nodiscard]] StellarHandle Load(std::string_view path);

        /// @brief Registers a module linked into the process and initializes it.
        /// @param info The description. It must stay valid until the module is unloaded.
        /// @return The handle of the module.
        /// @throws Failures::NativeException if the module is not valid or its initialization fails.
        [[nodiscard]] StellarHandle Add(Link<const StellarModuleInfo> info);

        /// @brief Shuts a module down and unloads it.
        /// @return False if the handle is stale.
        bool Unload(StellarHandle module);

        /// @brief Gets the name of a module.
        /// @return The name, or nothing if the handle is stale.
        [[nodiscard]] std::optional<std::string> GetName(StellarHandle module) const;

        /// @brief Gets the number of loaded modules.
        [[nodiscard]] std::size_t size() const;
    };
}
