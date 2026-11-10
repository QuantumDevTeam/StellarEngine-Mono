// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/Rasterizer.h
/// @brief Rasterizer state enumerations.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Polygon fill mode.
    enum class API FillMode : uint32_t
    {
        Wireframe, ///< Draw polygon edges only (debug/editor visualization).
        Solid,     ///< Fill polygon interiors (normal rendering).
    };

    /// @brief Which triangle facing is discarded during rasterization.
    enum class API CullMode : uint32_t
    {
        None,  ///< Draw both front and back faces.
        Front, ///< Cull front-facing triangles.
        Back,  ///< Cull back-facing triangles (default).
    };

    /// @brief Winding order that identifies a front-facing triangle.
    enum class API FrontFace : uint32_t
    {
        Clockwise,        ///< Clockwise vertices are front-facing.
        CounterClockwise, ///< Counter-clockwise vertices are front-facing.
    };

    /// @brief Enables conservative rasterization, which covers partially covered pixels.
    enum class API ConservativeRaster : uint32_t
    {
        Off, ///< Standard sample-based coverage.
        On,  ///< Conservative coverage when supported by the backend.
    };
}
