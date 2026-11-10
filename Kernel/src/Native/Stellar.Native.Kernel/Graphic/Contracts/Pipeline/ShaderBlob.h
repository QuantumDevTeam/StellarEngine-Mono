// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Pipeline/ShaderBlob.h
/// @brief Non-owning view of compiled shader bytecode.
#pragma once

#include <span>
#include <string>

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Compiled shader bytecode plus its entry point and debug name.
    ///
    /// The blob does not own the bytecode: the span must remain valid until the
    /// pipeline that consumes it has been created. ShaderBlob is a plain value
    /// type and can be copied freely.
    struct API ShaderBlob
    {
        std::span<const std::byte> bytecode{}; ///< Compiled bytecode; empty when the stage is unused.
        std::string entryPoint{"main"};    ///< Entry-point name exported by the bytecode.
        std::string debugName{};               ///< Name shown by validation and profiling tools.

        /// @brief True when the blob contains no bytecode (stage is unused).
        /// @return True when the bytecode span is empty.
        [[nodiscard]] bool IsEmpty() const noexcept { return bytecode.empty(); }
        /// @brief Size of the bytecode in bytes.
        /// @return Number of bytes in the bytecode span.
        [[nodiscard]] std::size_t Size() const noexcept { return bytecode.size(); }
    };
}
