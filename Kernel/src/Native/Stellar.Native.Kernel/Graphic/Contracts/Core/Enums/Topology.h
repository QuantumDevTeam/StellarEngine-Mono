// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/Topology.h
/// @brief Primitive topologies for input assembly.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Primitive topology used to assemble vertices into primitives.
    ///
    /// Adjacency variants pass extra vertices to geometry shaders; PatchList
    /// is used with tessellation and requires a patch control-point count.
    enum class API Topology : uint32_t
    {
        Undefined = 0,            ///< No topology; drawing is invalid.
        PointList,                ///< Each vertex is an independent point.
        LineList,                 ///< Each pair of vertices forms a line.
        LineStrip,                ///< Consecutive vertices form a connected line strip.
        TriangleList,             ///< Each group of three vertices forms a triangle.
        TriangleStrip,            ///< Consecutive vertices form a connected triangle strip.
        LineListAdjacency,        ///< Line list with adjacency information for geometry shaders.
        LineStripAdjacency,       ///< Line strip with adjacency information.
        TriangleListAdjacency,    ///< Triangle list with adjacency information.
        TriangleStripAdjacency,   ///< Triangle strip with adjacency information.
        PatchList,                ///< Control points consumed by the tessellation stages.
    };

    /// @brief Coarse primitive family derived from a Topology.
    enum class API TopologyType : uint32_t
    {
        Undefined = 0, ///< Unknown or unset family.
        Point,         ///< Point primitives.
        Line,          ///< Line primitives.
        Triangle,      ///< Triangle primitives.
        Patch,         ///< Tessellation patches.
    };
}
