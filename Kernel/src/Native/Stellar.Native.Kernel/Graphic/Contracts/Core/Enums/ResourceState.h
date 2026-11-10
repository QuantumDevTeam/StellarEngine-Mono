// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>
#include <type_traits>

/// @file Graphic/Contracts/Core/Enums/ResourceState.h
/// @brief Resource usage states used for barriers and transitions.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Describes how a resource is currently used by the GPU.
    ///
    /// States are bit flags because a resource may be readable by several
    /// shader stages at once. Before recording commands that use a resource in
    /// a different way, call ICommandList::Transition so the backend can emit
    /// the required barrier or cache flush.
    ///
    /// @note This is a portable superset. Backends map each flag to their
    /// native state; a state that has no native equivalent makes the
    /// transition fail validation.
    enum class API ResourceState : uint32_t
    {
        Common = 0, ///< No pending use; the resource can be re-purposed.

        VertexAndConstantBuffer = 1u << 0,  ///< Read as vertex or constant buffer data.
        IndexBuffer             = 1u << 1,  ///< Read as index buffer data.
        RenderTarget            = 1u << 2,  ///< Bound as a color render target.
        UnorderedAccess         = 1u << 3,  ///< Bound as an unordered access view (read/write).

        DepthWrite = 1u << 4,  ///< Bound as a depth/stencil target with writes enabled.
        DepthRead  = 1u << 5,  ///< Sampled or tested as depth/stencil data without writes.

        NonPixelShaderResource = 1u << 6,  ///< Read by vertex/geometry/hull/domain/compute shaders.
        PixelShaderResource    = 1u << 7,  ///< Read by the pixel shader.
        AllShaderResource      = NonPixelShaderResource | PixelShaderResource, ///< Read by any shader stage.

        StreamOut        = 1u << 8,   ///< Written by stream-output stage.
        IndirectArgument = 1u << 9,   ///< Read as indirect draw/dispatch arguments.
        CopyDest         = 1u << 10,  ///< Destination of a copy or upload.
        CopySource       = 1u << 11,  ///< Source of a copy or readback.
        ResolveDest      = 1u << 12,  ///< Destination of a multisample resolve.
        ResolveSource    = 1u << 13,  ///< Source of a multisample resolve.
        Present          = 1u << 14,  ///< Back buffer ready for presentation.
        Predication      = 1u << 15,  ///< Read as a predication buffer.

        /// @brief Read-only combination suitable for most buffers.
        GenericRead = VertexAndConstantBuffer
                      | IndexBuffer
                      | AllShaderResource
                      | IndirectArgument
                      | CopySource,

        // RT-specific states (reserved for ray tracing).
        RaytracingStructure = 1u << 22,  ///< Acceleration structure used by ray-tracing shaders.
        ShadingRateSource   = 1u << 24,  ///< Shading-rate source image.

        // Video states.
        VideoDecodeRead   = 1u << 16,  ///< Read by the video decoder.
        VideoDecodeWrite  = 1u << 17,  ///< Written by the video decoder.
        VideoProcessRead  = 1u << 18,  ///< Read by the video processor.
        VideoProcessWrite = 1u << 19,  ///< Written by the video processor.
        VideoEncodeRead   = 1u << 21,  ///< Read by the video encoder.
        VideoEncodeWrite  = 1u << 23,  ///< Written by the video encoder.
    };
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    /// @brief Enables bitwise flag operators for Graphics::ResourceState.
    template <> struct IsFlagEnum<ResourceState> : std::true_type {};
}
