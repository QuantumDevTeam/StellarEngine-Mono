// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Core/Tags.h
/// @brief Empty tag types used to parameterize handles and classify resources.
#pragma once

#include <type_traits>

/// @brief Compile-time tags and category traits for graphics handles.
namespace Stellar::Native::Kernel::Graphics::Tags
{
    // buffers

    /// @brief Tag for an untyped GPU buffer handle.
    struct Buffer {};

    /// @brief Tag for a vertex buffer handle.
    struct VertexBuffer {};

    /// @brief Tag for an index buffer handle.
    struct IndexBuffer {};

    /// @brief Tag for a constant buffer handle.
    struct ConstantBuffer {};

    /// @brief Tag for a structured buffer handle.
    struct StructuredBuffer {};

    /// @brief Tag for a CPU-writable upload buffer handle.
    struct UploadBuffer {};

    /// @brief Tag for a CPU-readable readback buffer handle.
    struct ReadbackBuffer {};

    // textures

    /// @brief Tag for an untyped texture handle.
    struct Texture {};

    /// @brief Tag for a 1D texture handle.
    struct Texture1D {};

    /// @brief Tag for a 2D texture handle.
    struct Texture2D {};

    /// @brief Tag for a 3D texture handle.
    struct Texture3D {};

    /// @brief Tag for a cube-map texture handle.
    struct TextureCube {};

    /// @brief Tag for a texture used as a render target.
    struct RenderTarget {};

    /// @brief Tag for a depth/stencil texture.
    struct DepthStencil {};

    // pipeline

    /// @brief Tag for a compiled shader handle.
    struct Shader {};

    /// @brief Tag for a pipeline-state object handle.
    struct PipelineState {};

    /// @brief Tag for a sampler-state handle.
    struct Sampler {};

    /// @brief Tag for a command-list handle.
    struct CommandList {};

    /// @brief Tag for a command-queue handle.
    struct CommandQueue {};

    /// @brief Tag for a GPU fence handle.
    struct Fence {};

    // context

    /// @brief Tag for a rendering-context handle.
    struct Context {};

    /// @brief Tag for a swap-chain handle.
    struct SwapChain {};

    // backend

    /// @brief Tag for a descriptor-heap handle.
    struct DescriptorHeap {};

    // Trait : Base

    /// @brief Root category for tags that identify GPU resources.
    struct ResourceTagBase {};

    /// @brief Category for buffer-like resource tags.
    struct BufferTagBase : ResourceTagBase {};

    /// @brief Category for texture-like resource tags.
    struct TextureTagBase : ResourceTagBase {};

    /// @brief Category for pipeline-related tags.
    ///
    /// Pipeline tags currently have no TagCategory specialization, so the
    /// category is only used when a caller opts into it explicitly.
    struct PipelineTagBase : ResourceTagBase {};

    // binding to category

    /// @brief Maps a tag type to its category base.
    ///
    /// The primary template maps unknown tags to `void`, which makes
    /// TagOf<Category>::Is<Tag> evaluate to false.
    template <typename> struct TagCategory
    {
        using type = void; ///< Unknown tags have no category.
    };

    // buffers

    /// @brief Buffer tags belong to the buffer category.
    template <> struct TagCategory<Buffer>
    {
        using type = BufferTagBase; ///< Category base for this tag.
    };

    /// @brief Vertex buffer tags belong to the buffer category.
    template <> struct TagCategory<VertexBuffer>
    {
        using type = BufferTagBase; ///< Category base for this tag.
    };

    /// @brief Index buffer tags belong to the buffer category.
    template <> struct TagCategory<IndexBuffer>
    {
        using type = BufferTagBase; ///< Category base for this tag.
    };

    /// @brief Constant buffer tags belong to the buffer category.
    template <> struct TagCategory<ConstantBuffer>
    {
        using type = BufferTagBase; ///< Category base for this tag.
    };

    /// @brief Structured buffer tags belong to the buffer category.
    template <> struct TagCategory<StructuredBuffer>
    {
        using type = BufferTagBase; ///< Category base for this tag.
    };

    /// @brief Upload buffer tags belong to the buffer category.
    template <> struct TagCategory<UploadBuffer>
    {
        using type = BufferTagBase; ///< Category base for this tag.
    };

    /// @brief Readback buffer tags belong to the buffer category.
    template <> struct TagCategory<ReadbackBuffer>
    {
        using type = BufferTagBase; ///< Category base for this tag.
    };

    // texture

    /// @brief Generic texture tags belong to the texture category.
    template <> struct TagCategory<Texture>
    {
        using type = TextureTagBase; ///< Category base for this tag.
    };

    /// @brief 1D texture tags belong to the texture category.
    template <> struct TagCategory<Texture1D>
    {
        using type = TextureTagBase; ///< Category base for this tag.
    };

    /// @brief 2D texture tags belong to the texture category.
    template <> struct TagCategory<Texture2D>
    {
        using type = TextureTagBase; ///< Category base for this tag.
    };

    /// @brief 3D texture tags belong to the texture category.
    template <> struct TagCategory<Texture3D>
    {
        using type = TextureTagBase; ///< Category base for this tag.
    };

    /// @brief Cube-map tags belong to the texture category.
    template <> struct TagCategory<TextureCube>
    {
        using type = TextureTagBase; ///< Category base for this tag.
    };

    /// @brief Render-target tags belong to the texture category.
    template <> struct TagCategory<RenderTarget>
    {
        using type = TextureTagBase; ///< Category base for this tag.
    };

    /// @brief Depth/stencil tags belong to the texture category.
    template <> struct TagCategory<DepthStencil>
    {
        using type = TextureTagBase; ///< Category base for this tag.
    };

    // pipeline
    // all tags have different categories

    // validation

    /// @brief Compile-time predicate: does @p Tag belong to @p Category?
    ///
    /// Example: `Tags::TagOf<Tags::BufferTagBase>::Is<Tags::VertexBuffer>` is
    /// true because TagCategory<VertexBuffer>::type derives from BufferTagBase.
    template <typename Category>
    struct TagOf
    {
        /// @brief True when the tag's category derives from @p Category.
        template <typename Tag>
        static constexpr bool Is = std::is_base_of_v<
            Category, typename TagCategory<Tag>::type>;
    };
}
