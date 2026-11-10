// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/TextureUsage.h
/// @brief CPU/GPU access patterns for textures.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Declares where a texture lives and how often the CPU touches it.
    ///
    /// The usage selects the native memory heap and the allowed update paths.
    /// It is independent from TextureFlags, which describe binding capabilities.
    enum class API TextureUsage : uint32_t
    {
        Default,   ///< GPU-resident memory; updated through copies or render passes.
        Immutable, ///< Initialized once at creation; the CPU cannot modify it afterwards.
        Dynamic,   ///< CPU-writable, typically updated every frame via Map.
        Staging,   ///< CPU read/write staging memory used to transfer data to or from the GPU.
    };
}
