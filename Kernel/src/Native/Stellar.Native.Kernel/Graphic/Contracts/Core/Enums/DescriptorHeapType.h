// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/DescriptorHeapType.h
/// @brief Descriptor heap categories used by descriptor-based backends.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Names the kind of descriptors stored in a descriptor heap.
    ///
    /// Backends without explicit heaps may map several categories to one
    /// native table. A heap can only contain descriptors of the selected kind.
    enum class API DescriptorHeapType : uint32_t
    {
        CBV_SRV_UAV, ///< Constant buffer, shader resource and unordered access views.
        Sampler,     ///< Sampler states.
        RTV,         ///< Render target views.
        DSV,         ///< Depth/stencil views.
    };
}
