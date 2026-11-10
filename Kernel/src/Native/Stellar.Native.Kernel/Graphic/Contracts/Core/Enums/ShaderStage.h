// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>
#include <type_traits>

/// @file Graphic/Contracts/Core/Enums/ShaderStage.h
/// @brief Graphics and compute shader stage flags.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Identifies one or more programmable pipeline stages.
    ///
    /// The enum is a bit mask so a single value can describe the visibility of
    /// a resource to several stages, for example ShaderStage::All. Backends
    /// translate the mask to their native stage flags or shader visibility.
    enum class API ShaderStage : uint32_t
    {
        None     = 0,        ///< No stage.
        Vertex   = 1u << 0,  ///< Vertex shader.
        Pixel    = 1u << 1,  ///< Pixel (fragment) shader.
        Compute  = 1u << 2,  ///< Compute shader.
        Geometry = 1u << 3,  ///< Geometry shader.
        Hull     = 1u << 4,  ///< Hull (tessellation control) shader.
        Domain   = 1u << 5,  ///< Domain (tessellation evaluation) shader.

        /// @brief All stages of the raster pipeline (vertex through pixel).
        AllGraphics = Vertex | Pixel | Geometry | Hull | Domain,
        /// @brief Every stage, including compute.
        All         = AllGraphics | Compute,
    };
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    /// @brief Enables bitwise flag operators for Graphics::ShaderStage.
    template <>
    struct IsFlagEnum<ShaderStage> : std::true_type {};
}
