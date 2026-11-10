// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Resource/Descriptions.h
/// @brief CPU-side descriptions for buffers, textures and transfer regions.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstddef>
#include <cstdint>
#include <span>
#include <string_view>

#include "Graphic/Contracts/Core/Enums.h"
#include "Graphic/Contracts/Core/Format.h"

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Creation parameters for a GPU buffer.
    ///
    /// The description combines size/stride information with the binding flags
    /// and memory usage. The static factory functions cover the common cases
    /// and fill in the matching flags and initial state.
    struct API BufferDesc
    {
        uint64_t size = 0;                    ///< Total buffer size in bytes.
        uint32_t stride = 0;                  ///< Distance between elements in bytes; 0 for raw buffers.
        uint32_t elementCount = 0;            ///< Number of elements; 0 when not element-structured.
        uint32_t alignment = 0;               ///< Required base alignment in bytes; 0 = backend default.
        Format format = Format::Unknown;      ///< Element format for typed buffers, otherwise Unknown.
        BufferFlags flags = BufferFlags::None;///< Binding capabilities (vertex, index, SRV, UAV, ...).
        BufferUsage usage = BufferUsage::Default; ///< Memory heap and CPU access pattern.
        ResourceState initialState = ResourceState::Common; ///< State before the first transition.

        std::string_view debugName{}; ///< Name shown by validation and profiling tools.

        /// @brief Builds a vertex buffer.
        /// @param count Number of vertices.
        /// @param stride Size of one vertex in bytes.
        /// @param usage Memory usage; Default keeps the buffer GPU-resident.
        /// @return Description with size = count * stride and VertexBuffer|ShaderResource flags.
        [[nodiscard]] static constexpr BufferDesc VertexBuffer(
            uint32_t count, uint32_t stride,
            BufferUsage usage = BufferUsage::Default) noexcept
        {
            BufferDesc d{};
            d.size = static_cast<uint64_t>(count) * stride;
            d.stride = stride;
            d.elementCount = count;
            d.flags = BufferFlags::VertexBuffer | BufferFlags::ShaderResource;
            d.usage = usage;
            d.initialState = ResourceState::VertexAndConstantBuffer;
            return d;
        }

        /// @brief Builds an index buffer.
        /// @param count Number of indices.
        /// @param fmt Index component width (16 or 32 bits).
        /// @param usage Memory usage; Default keeps the buffer GPU-resident.
        /// @return Description with the matching stride, format and IndexBuffer flag.
        [[nodiscard]] static constexpr BufferDesc IndexBuffer(
            uint32_t count, IndexFormat fmt,
            BufferUsage usage = BufferUsage::Default) noexcept
        {
            const uint32_t sz = fmt == IndexFormat::UInt16 ? 2u : 4u;
            BufferDesc d{};
            d.size = static_cast<uint64_t>(count) * sz;
            d.stride = sz;
            d.elementCount = count;
            d.format = fmt == IndexFormat::UInt16 ? Format::R16_UInt : Format::R32_UInt;
            d.flags = BufferFlags::IndexBuffer;
            d.usage = usage;
            d.initialState = ResourceState::IndexBuffer;
            return d;
        }

        /// @brief Builds a constant buffer, rounding the size up to the alignment.
        /// @param sizeBytes Minimum size in bytes.
        /// @param usage Memory usage; Dynamic is suitable for per-frame constants.
        /// @param alignment Required constant-buffer alignment (16 by default).
        /// @return Description whose size is a multiple of @p alignment.
        [[nodiscard]] static constexpr BufferDesc ConstantBuffer(
            uint32_t sizeBytes,
            BufferUsage usage = BufferUsage::Dynamic,
            uint8_t alignment = 1u << 4) noexcept
        {
            BufferDesc d{};
            d.size = static_cast<uint64_t>(sizeBytes) + (alignment - 1u) & ~uint64_t{(alignment - 1u)};
            d.stride = 0;
            d.elementCount = 1;
            d.alignment = alignment;
            d.flags = BufferFlags::ConstantBuffer;
            d.usage = usage;
            d.initialState = ResourceState::VertexAndConstantBuffer;
            return d;
        }

        /// @brief Builds a structured buffer with a fixed element stride.
        /// @param elementCount Number of elements.
        /// @param stride Size of one element in bytes.
        /// @param extra Additional binding flags (for example UnorderedAccess).
        /// @param usage Memory usage.
        /// @return Description with the StructuredBuffer flag and the requested extras.
        [[nodiscard]] static constexpr BufferDesc Structured(
            uint32_t elementCount, uint32_t stride,
            BufferFlags extra = BufferFlags::None,
            BufferUsage usage = BufferUsage::Default) noexcept
        {
            BufferDesc d{};
            d.size = static_cast<uint64_t>(elementCount) * stride;
            d.stride = stride;
            d.elementCount = elementCount;
            d.flags = BufferFlags::StructuredBuffer | extra;
            d.usage = usage;
            d.initialState = ResourceState::Common;
            return d;
        }

        /// @brief Builds a CPU-writable buffer for per-frame uploads.
        /// @param sizeBytes Buffer size in bytes.
        /// @return Dynamic buffer initially readable by the GPU.
        [[nodiscard]] static constexpr BufferDesc Upload(uint64_t sizeBytes) noexcept
        {
            BufferDesc d{};
            d.size = sizeBytes;
            d.usage = BufferUsage::Dynamic;
            d.initialState = ResourceState::GenericRead;
            return d;
        }

        /// @brief Builds a staging buffer readable by the CPU after a copy.
        /// @param sizeBytes Buffer size in bytes.
        /// @return Staging buffer initially in the CopyDest state.
        [[nodiscard]] static constexpr BufferDesc Readback(uint64_t sizeBytes) noexcept
        {
            BufferDesc d{};
            d.size = sizeBytes;
            d.usage = BufferUsage::Staging;
            d.initialState = ResourceState::CopyDest;
            return d;
        }
    };

    // COMMENT
    struct API BufferUploadDesc
    {
        std::span<const std::byte> bytes{};
        uint64_t dstOffset = 0;
    };

    /// @brief Creation parameters for a GPU texture.
    ///
    /// The description covers dimensionality, dimensions, mip/array layout,
    /// multisampling, format and binding capabilities. Use the static helpers
    /// for render targets, depth buffers and cube maps.
    struct API TextureDesc
    {
        TextureType type = TextureType::Texture2D; ///< Texture dimensionality.
        uint32_t width = 1;                 ///< Width in texels of mip level 0.
        uint32_t height = 1;                ///< Height in texels of mip level 0.
        uint32_t depth = 1;                 ///< Depth in texels for 3D textures.
        uint32_t mipLevels = 1;             ///< Number of mip levels, including the base level.
        // For TextureCube this is a multiple of 6 (one slice per face).
        uint32_t arraySize = 1;             ///< Number of array slices; cube maps use a multiple of 6.
        uint32_t sampleCount = 1;           ///< Samples per texel (1 = no MSAA).
        uint32_t sampleQuality = 0;         ///< Backend-specific quality level within sampleCount.
        Format format = Format::RGBA8_UNorm;///< Pixel format of the texture.
        TextureFlags flags = TextureFlags::None; ///< Binding capabilities (RTV, DSV, UAV, SRV, ...).
        TextureUsage usage = TextureUsage::Default; ///< Memory heap and CPU access pattern.
        ResourceState initialState = ResourceState::Common; ///< State before the first transition.

        std::string_view debugName{}; ///< Name shown by validation and profiling tools.

        /// @brief Builds a render-target texture that can also be sampled.
        /// @param w Width in pixels.
        /// @param h Height in pixels.
        /// @param fmt Render-target pixel format.
        /// @return Texture initially in the RenderTarget state.
        [[nodiscard]] static constexpr TextureDesc RenderTarget(
            uint32_t w, uint32_t h, Format fmt) noexcept
        {
            TextureDesc d{};
            d.width = w;
            d.height = h;
            d.format = fmt;
            d.flags = TextureFlags::AllowRenderTarget | TextureFlags::AllowShaderResource;
            d.usage = TextureUsage::Default;
            d.initialState = ResourceState::RenderTarget;
            return d;
        }

        /// @brief Builds a depth/stencil texture that can also be sampled.
        /// @param w Width in pixels.
        /// @param h Height in pixels.
        /// @param fmt Depth/stencil pixel format.
        /// @return Texture initially in the DepthWrite state.
        [[nodiscard]] static constexpr TextureDesc DepthStencil(
            uint32_t w, uint32_t h, Format fmt) noexcept
        {
            TextureDesc d{};
            d.width = w;
            d.height = h;
            d.format = fmt;
            d.flags = TextureFlags::AllowDepthStencil | TextureFlags::AllowShaderResource;
            d.initialState = ResourceState::DepthWrite;
            return d;
        }

        /// @brief Builds a cube-map texture.
        /// @param size Edge length in texels (width = height = size).
        /// @param fmt Pixel format.
        /// @param mipLevels Number of mip levels, including the base level.
        /// @return Texture with six array slices, one per cube face.
        [[nodiscard]] static constexpr TextureDesc Cube(
            uint32_t size, Format fmt, uint32_t mipLevels = 1) noexcept
        {
            TextureDesc d{};
            d.type = TextureType::TextureCube;
            d.width = size;
            d.height = size;
            d.mipLevels = mipLevels;
            d.arraySize = 6;
            d.format = fmt;
            d.flags = TextureFlags::AllowShaderResource;
            d.usage = TextureUsage::Default;
            d.initialState = ResourceState::Common;
            return d;
        }
    };

    /// @brief Mip level and array slice identifying one texture subresource.
    struct API TextureSubresource
    {
        uint32_t mipLevel = 0;  ///< Mip level index; 0 is the base level.
        uint32_t arraySlice = 0;///< Array slice index; cube faces are stored as consecutive slices.
    };

    /// @brief Subresource plus a texel-space box used by copy/upload operations.
    struct API TextureRegion
    {
        uint32_t mipLevel = 0;  ///< Mip level to access.
        uint32_t arraySlice = 0;///< Array slice (or cube face) to access.
        uint32_t x = 0;         ///< Left edge of the box in texels.
        uint32_t y = 0;         ///< Top edge of the box in texels.
        uint32_t z = 0;         ///< Front edge of the box in texels (3D textures).
        uint32_t width = 1;     ///< Box width in texels.
        uint32_t height = 1;    ///< Box height in texels.
        uint32_t depth = 1;     ///< Box depth in texels.
    };

    // COMMENT
    struct API TextureUploadDesc
    {
        std::span<const std::byte> data{};
        TextureRegion region{};
        uint32_t rowPitch = 0;
        uint32_t slicePitch = 0;
    };

    /// @brief One stream-output buffer binding.
    struct API StreamOutputTarget
    {
        Ptr<Resource::IBuffer> buffer{}; ///< Buffer that receives stream-output data; must be non-null.
        uint32_t offset = 0;             ///< Byte offset at which writes begin.
    };
}
