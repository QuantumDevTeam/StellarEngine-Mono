// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Core/Handles.h
/// @brief Generation-tagged handles and the slot pool that validates them.
#pragma once

#include <cstdint>
#include <vector>

#include "Graphic/Contracts/Core/Tags.h"

/// @brief Generation-tagged handles and their slot pool.
namespace Stellar::Native::Kernel::Graphics::Handles
{
    /// @brief Typed, generation-tagged identifier for a GPU object.
    ///
    /// The 64-bit `id` packs a 32-bit slot index and a 32-bit generation.
    /// When a slot is freed and reused, its generation changes, so an old
    /// handle can be detected instead of silently referring to a new object.
    /// A default-constructed handle is invalid.
    ///
    /// @tparam Tag Tag type that documents which kind of object the handle
    ///         refers to (see Graphics::Tags).
    template <typename Tag> // NOLINT
    struct API Handle
    {
        uint64_t id = 0; ///< Packed slot index (low 32 bits) and generation (high 32 bits).

        /// @brief True when the handle refers to a slot (generation is encoded in id).
        /// @return True when the handle is non-null.
        [[nodiscard]] constexpr bool IsValid() const noexcept { return id != 0; }

        /// @brief Same as IsValid(), for use in boolean contexts.
        /// @return True when the handle is non-null.
        constexpr explicit operator bool() const noexcept { return IsValid(); }

        /// @brief Clears the handle without touching the slot it refers to.
        constexpr void Invalidate() noexcept { id = 0; }

        /// @brief Orders handles by their packed id (stable for sorting/debugging).
        friend constexpr auto operator<=>(Handle, Handle) noexcept = default;
    };

    // specification

    // buffers
    /// @brief Handle to any buffer resource.
    using BufferHandle = Handle<Tags::Buffer>;
    /// @brief Handle to a vertex buffer.
    using VertexBufferHandle = Handle<Tags::VertexBuffer>;
    /// @brief Handle to an index buffer.
    using IndexBufferHandle = Handle<Tags::IndexBuffer>;
    /// @brief Handle to a constant buffer.
    using ConstantBufferHandle = Handle<Tags::ConstantBuffer>;
    /// @brief Handle to a structured buffer.
    using StructuredBufferHandle = Handle<Tags::StructuredBuffer>;
    /// @brief Handle to a CPU-writable upload buffer.
    using UploadBufferHandle = Handle<Tags::UploadBuffer>;
    /// @brief Handle to a CPU-readable readback buffer.
    using ReadbackBufferHandle = Handle<Tags::ReadbackBuffer>;

    // texture
    /// @brief Handle to any texture resource.
    using TextureHandle = Handle<Tags::Texture>;
    /// @brief Handle to a 1D texture.
    using Texture1DHandle = Handle<Tags::Texture1D>;
    /// @brief Handle to a 2D texture.
    using Texture2DHandle = Handle<Tags::Texture2D>;
    /// @brief Handle to a 3D texture.
    using Texture3DHandle = Handle<Tags::Texture3D>;
    /// @brief Handle to a cube-map texture.
    using TextureCubeHandle = Handle<Tags::TextureCube>;
    /// @brief Handle to a texture used as a color render target.
    using RenderTargetHandle = Handle<Tags::RenderTarget>;
    /// @brief Handle to a depth/stencil texture.
    using DepthStencilHandle = Handle<Tags::DepthStencil>;

    // pipeline
    /// @brief Handle to a compiled shader.
    using ShaderHandle = Handle<Tags::Shader>;
    /// @brief Handle to a graphics or compute pipeline state.
    using PipelineStateHandle = Handle<Tags::PipelineState>;
    /// @brief Handle to a sampler state.
    using SamplerHandle = Handle<Tags::Sampler>;

    // Command
    /// @brief Handle to a command list.
    using CommandListHandle = Handle<Tags::CommandList>;
    /// @brief Handle to a command queue.
    using CommandQueueHandle = Handle<Tags::CommandQueue>;

    // backend
    /// @brief Handle to a GPU fence.
    using FenceHandle = Handle<Tags::Fence>;
    /// @brief Handle to a swap chain.
    using SwapChainHandle = Handle<Tags::SwapChain>;
    /// @brief Handle to a descriptor heap.
    using DescriptorHeapHandle = Handle<Tags::DescriptorHeap>;

    /// @brief Allocates and validates generation-tagged ids.
    ///
    /// The pool owns no GPU objects; it only tracks which ids are alive. Free
    /// slots are recycled with a new generation, which invalidates old handles.
    /// @note Not thread-safe; callers provide external synchronization.
    class HandlePool
    {
        /// @brief One reusable id slot.
        struct Slot // NOLINT(clang-diagnostic-padded)
        {
            uint32_t generation = 1; ///< Incremented every time the slot is freed.
            bool alive = false;      ///< True while an id refers to this slot.
        };

        std::vector<Slot> _slots;      ///< Slot storage; indexed by the low 32 bits of an id.
        std::vector<uint32_t> _freeList; ///< Indices of slots available for reuse.
        uint32_t _next = 0;            ///< Number of slots ever created.

        /// @brief Extracts the slot index from a packed id.
        [[nodiscard]] static constexpr uint32_t IndexOf(uint64_t id) noexcept
        {
            return static_cast<uint32_t>(id & 0xFFFFFFFFu);
        }

        /// @brief Extracts the generation from a packed id.
        [[nodiscard]] static constexpr uint32_t GenerationOf(uint64_t id) noexcept
        {
            return static_cast<uint32_t>(id >> 32);
        }

    public:
        /// @brief Reserves a slot and returns a generation-tagged id.
        /// @return An id that stays unique even after the slot is reused.
        [[nodiscard]] uint64_t Allocate() noexcept
        {
            uint32_t idx;
            if (!_freeList.empty())
            {
                idx = _freeList.back();
                _freeList.pop_back();
            }
            else
            {
                idx = _next++;
                _slots.emplace_back();
            }
            _slots[idx].alive = true;
            return static_cast<uint64_t>(_slots[idx].generation) << 32 | idx;
        }

        /// @brief Checks whether an id still refers to a live object.
        /// @param id Packed handle id.
        /// @return True when the slot is alive and the generation matches.
        [[nodiscard]] bool IsAlive(uint64_t id) const noexcept
        {
            const uint32_t idx = IndexOf(id);
            return idx < _slots.size()
                   && _slots[idx].alive
                   && _slots[idx].generation == GenerationOf(id);
        }

        /// @brief Frees the slot referenced by @p id.
        /// @param id Packed handle id.
        /// @return True when the id was alive and has been freed.
        /// @note Stale or forged ids are rejected instead of corrupting the pool.
        [[nodiscard]] bool Free(uint64_t id) noexcept
        {
            if (!IsAlive(id))
                return false;

            const uint32_t idx = IndexOf(id);
            _slots[idx].alive = false;
            ++_slots[idx].generation;
            _freeList.push_back(idx);
            return true;
        }

        /// @brief Number of slots ever allocated, including currently free ones.
        /// @return Total number of allocated slots.
        [[nodiscard]] uint32_t SlotCount() const noexcept { return _next; }
    };
}
