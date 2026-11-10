// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/MapMode.h
/// @brief CPU access modes for Map/Unmap on buffers and textures.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Declares how the CPU intends to access a mapped resource.
    ///
    /// The mode is a promise to the backend and lets it choose the cheapest
    /// synchronization. Writing through a mode that does not permit writes is
    /// undefined behavior; reading through a write-only mode is likewise invalid.
    enum class API MapMode : uint32_t
    {
        Read,             ///< Read-only access; the previous contents are preserved.
        Write,            ///< Write-only access; the previous contents are preserved.
        ReadWrite,        ///< Read and write access; the previous contents are preserved.
        WriteDiscard,     ///< Write-only access; the previous contents may be discarded.
        WriteNoOverwrite, ///< Write-only access to regions not yet used by the GPU; may avoid a stall.
    };
}
