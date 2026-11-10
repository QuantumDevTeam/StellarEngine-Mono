// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/BufferUsage.h
/// @brief CPU/GPU access patterns for buffers.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Declares where a buffer lives and how often the CPU touches it.
    ///
    /// The usage selects the native memory heap and the allowed mapping modes.
    /// It is independent from BufferFlags, which describe how the buffer is
    /// bound to the pipeline.
    enum class API BufferUsage : uint32_t
    {
        Default,   ///< GPU-resident memory, updated rarely and usually via copies.
        Immutable, ///< Initialized once at creation; the CPU cannot modify it afterwards.
        Dynamic,   ///< CPU-writable every frame; usually persistently mapped.
        Staging,   ///< CPU read/write staging memory used to transfer data to or from the GPU.
    };
}
