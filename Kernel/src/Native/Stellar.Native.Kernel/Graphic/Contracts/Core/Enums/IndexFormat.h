// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/IndexFormat.h
/// @brief Index component widths for indexed draw calls.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Width and interpretation of indices stored in an index buffer.
    enum class API IndexFormat : uint32_t
    {
        UInt16, ///< 16-bit unsigned indices; supports up to 65,535 vertices per draw.
        UInt32, ///< 32-bit unsigned indices; required for larger vertex buffers.
    };
}
