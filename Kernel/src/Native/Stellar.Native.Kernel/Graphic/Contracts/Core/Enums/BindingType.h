// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/BindingType.h
/// @brief Shader binding categories used by pipeline binding layouts.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Classifies a shader input binding by its resource view type.
    ///
    /// A binding layout is a list of entries; each entry names one of these
    /// categories, the shader stage that consumes it and the register slot.
    /// The backend maps the pair (type, slot) to its native descriptor model.
    enum class API BindingType : uint32_t
    {
        ConstantBuffer,  ///< Constant buffer view (CBV) containing shader constants.
        ShaderResource,  ///< Shader resource view (SRV): a read-only buffer or texture.
        UnorderedAccess, ///< Unordered access view (UAV): a read/write buffer or texture.
        Sampler,         ///< Sampler state used to filter texture reads.
    };
}
