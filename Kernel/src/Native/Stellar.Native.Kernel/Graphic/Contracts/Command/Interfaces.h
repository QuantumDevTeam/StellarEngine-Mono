// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Command/Interfaces.h
/// @brief Command recording, submission, queries and fences.
#pragma once

#include <span>

#include "Graphic/Types.h" // NOLINT
#include "Graphic/Contracts/Core/Enums.h"
#include "Graphic/Contracts/Core/Format.h"
#include "Graphic/Contracts/Resource/Descriptions.h"

#include "Math/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Graphics::Command
{
    /// @brief GPU query object used to measure occlusion or timestamps.
    ///
    /// A query is recorded into a command list between BeginQuery() and
    /// EndQuery(). Results become available after the GPU has executed the
    /// commands; getResult() blocks until then.
    class API IQuery // NOLINT
    {
    public:
        virtual ~IQuery() = default;

        /// @brief Returns the device that created this query.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;

        /// @brief Returns the query kind.
        /// @return Query type enum value.
        [[nodiscard]] virtual QueryType getType() const noexcept = 0;

        /// @brief Blocks until the query result is available and returns it.
        /// @return Occlusion sample count, binary 0/1, or a timestamp in device ticks.
        /// @note Must not be called before the query has been submitted and completed.
        [[nodiscard]] virtual uint64_t getResult() const = 0;
    };

    must_be_abstract(IQuery)

    /// @brief Records GPU commands into a command buffer.
    ///
    /// The recording lifecycle is Begin() -> record commands -> End(). A list
    /// can be reused after Reset(). Resource state changes must be declared
    /// with Transition() before the resource is used in a new way. Methods that
    /// bind resources expect non-null pointers unless explicitly documented
    /// otherwise; passing null unbinds the corresponding slot where supported.
    class API ICommandList // NOLINT
    {
    public:
        virtual ~ICommandList() = default;

        /// @brief Returns the device that created this command list.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;

        // frame

        /// @brief Opens the command list for recording.
        virtual void Begin() = 0;
        /// @brief Closes the command list; it can then be submitted to a queue.
        virtual void End() = 0;
        /// @brief Resets the list to its initial empty state for reuse.
        /// @note The list must not be in the recording state or pending execution.
        virtual void Reset() = 0;

        // pipeline

        /// @brief Binds a graphics or compute pipeline state.
        /// @param pso Pipeline to bind for subsequent draw/dispatch commands.
        virtual void SetPipeline(Link<const Pipeline::IPipelineState> pso) = 0;
        /// @brief Sets the viewport used by subsequent draws.
        /// @param vp Viewport to apply.
        virtual void SetViewport(Link<const Viewport> vp) = 0;
        /// @brief Sets the scissor rectangle used by subsequent draws.
        /// @param sc Scissor rectangle to apply.
        virtual void SetScissor(Link<const Math::Shapes::ScissorRect<int32_t>> sc) = 0;
        /// @brief Sets the input-assembly primitive topology.
        /// @param t Topology to apply.
        virtual void SetTopology(Topology t) = 0;

        // GPU resource commands

        // Null render target slots unbind the corresponding output.
        /// @brief Binds color render targets and an optional depth/stencil target.
        /// @param targets Render-target textures; a null entry unbinds that slot.
        /// @param dsv Depth/stencil texture, or null to disable depth/stencil output.
        virtual void SetRenderTargets(std::span<const Ptr<Resource::ITexture>> targets,
                                      Ptr<Resource::ITexture> dsv) = 0;

        /// @brief Clears a color render target to a constant color.
        /// @param target Texture to clear; must be bound as a render target.
        /// @param color Clear value.
        virtual void ClearRenderTarget(Ptr<const Resource::ITexture> target,
                                       Link<const Color4> color) = 0;

        /// @brief Clears a depth/stencil target.
        /// @param dsv Depth/stencil texture to clear.
        /// @param depth Depth value written to every texel.
        /// @param stencil Stencil value written to every texel.
        virtual void ClearDepthStencil(Ptr<const Resource::ITexture> dsv,
                                       float depth, uint8_t stencil) = 0;

        /// @brief Binds a vertex buffer to an input slot.
        /// @param vb Buffer containing vertex data.
        /// @param slot Input slot index declared by the pipeline input layout.
        /// @param stride Distance between consecutive vertices in bytes.
        /// @param offset Byte offset of the first vertex in the buffer.
        virtual void SetVertexBuffer(Ptr<const Resource::IBuffer> vb, uint32_t slot,
                                     uint32_t stride, uint32_t offset) = 0;

        /// @brief Binds an index buffer.
        /// @param ib Buffer containing indices.
        /// @param format Index component format (16- or 32-bit).
        /// @param offset Byte offset of the first index in the buffer.
        virtual void SetIndexBuffer(Ptr<const Resource::IBuffer> ib, Format format,
                                    uint32_t offset) = 0;

        /// @brief Binds a constant buffer to a shader stage.
        /// @param cb Buffer containing constants.
        /// @param slot Constant-buffer register slot.
        /// @param stage Shader stage(s) that can read the buffer.
        virtual void SetConstantBuffer(Ptr<const Resource::IBuffer> cb, uint32_t slot,
                                       ShaderStage stage) = 0;

        /// @brief Binds a buffer as a shader resource (SRV).
        /// @param srv Read-only buffer.
        /// @param slot Shader resource register slot.
        /// @param stage Shader stage(s) that can read the buffer.
        virtual void SetShaderResource(Ptr<const Resource::IBuffer> srv, uint32_t slot,
                                       ShaderStage stage) = 0;

        /// @brief Binds a buffer as an unordered access view (UAV).
        /// @param uav Read/write buffer.
        /// @param slot UAV register slot.
        /// @param stage Shader stage(s) that can access the buffer.
        virtual void SetUnorderedAccess(Ptr<Resource::IBuffer> uav, uint32_t slot,
                                        ShaderStage stage) = 0;

        /// @brief Binds a texture as a shader resource (SRV).
        /// @param srv Read-only texture.
        /// @param slot Shader resource register slot.
        /// @param stage Shader stage(s) that can read the texture.
        virtual void SetShaderResourceTexture(Ptr<const Resource::ITexture> srv, uint32_t slot,
                                              ShaderStage stage) = 0;

        /// @brief Binds a texture as an unordered access view (UAV).
        /// @param uav Read/write texture.
        /// @param slot UAV register slot.
        /// @param stage Shader stage(s) that can access the texture.
        virtual void SetUnorderedAccessTexture(Ptr<Resource::ITexture> uav, uint32_t slot,
                                               ShaderStage stage) = 0;

        /// @brief Draws non-indexed primitives.
        /// @param vertexCount Number of vertices to draw.
        /// @param startVertex Index of the first vertex in the bound vertex buffer.
        virtual void Draw(uint32_t vertexCount, uint32_t startVertex) = 0;

        /// @brief Draws indexed primitives.
        /// @param indexCount Number of indices to draw.
        /// @param startIndex Index of the first index in the bound index buffer.
        /// @param baseVertex Value added to every index before vertex fetch.
        virtual void DrawIndexed(uint32_t indexCount, uint32_t startIndex,
                                 int32_t baseVertex) = 0;

        /// @brief Draws instanced non-indexed primitives.
        /// @param vertexCount Vertices per instance.
        /// @param instanceCount Number of instances.
        /// @param startVertex First vertex in the vertex buffer.
        /// @param startInstance First instance identifier.
        virtual void DrawInstanced(uint32_t vertexCount, uint32_t instanceCount,
                                   uint32_t startVertex, uint32_t startInstance) = 0;

        /// @brief Draws instanced indexed primitives.
        /// @param indexCount Indices per instance.
        /// @param instanceCount Number of instances.
        /// @param startIndex First index in the index buffer.
        /// @param baseVertex Value added to every index before vertex fetch.
        /// @param startInstance First instance identifier.
        virtual void DrawIndexedInstanced(uint32_t indexCount, uint32_t instanceCount,
                                          uint32_t startIndex, int32_t baseVertex,
                                          uint32_t startInstance) = 0;

        /// @brief Dispatches a compute shader with a three-dimensional thread-group grid.
        /// @param groupCountX Number of groups along X.
        /// @param groupCountY Number of groups along Y.
        /// @param groupCountZ Number of groups along Z.
        virtual void Dispatch(uint32_t groupCountX, uint32_t groupCountY, uint32_t groupCountZ) = 0;

        /// @brief Dispatches a compute shader using arguments stored in a buffer.
        /// @param args Buffer containing the native indirect dispatch arguments.
        /// @param offset Byte offset of the argument structure.
        virtual void DispatchIndirect(Ptr<const Resource::IBuffer> args, uint64_t offset) = 0;

        /// @brief Sets the constant blend factor used by SimpleBlendFactor/AlphaFactor.
        /// @param factor Blend factor color.
        virtual void SetBlendFactor(Link<const Color4> factor) = 0;
        /// @brief Sets the stencil reference value used by stencil tests.
        /// @param stencilRef Reference value for stencil comparisons.
        virtual void SetStencilRef(uint32_t stencilRef) = 0;

        /// @brief Enables conditional execution of subsequent commands.
        /// @param buffer Buffer containing the predicate value; null disables predication.
        /// @param offset Byte offset of the predicate value.
        virtual void SetPredication(Ptr<const Resource::IBuffer> buffer, uint64_t offset) = 0;

        /// @brief Binds stream-output targets for the geometry shader stage.
        /// @param targets Array of stream output target descriptions.
        virtual void SetStreamOutputTargets(std::span<const StreamOutputTarget> targets) = 0;

        /// @brief Starts a query.
        /// @param query Query to begin; must be created for the matching type.
        virtual void BeginQuery(Ptr<IQuery> query) = 0;
        /// @brief Ends a query previously started with BeginQuery().
        /// @param query Query to end.
        virtual void EndQuery(Ptr<IQuery> query) = 0;

        /// @brief Transitions a resource to a new usage state.
        /// @param res Resource to transition.
        /// @param newState State required by subsequent commands.
        /// @note The backend may split this into one or more native barriers.
        virtual void Transition(Ptr<Resource::IResource> res,
                                ResourceState newState) = 0;

        /// @brief Copies the entire contents of one resource into another.
        /// @param dst Destination resource; must be compatible with @p src.
        /// @param src Source resource.
        virtual void CopyResource(Ptr<Resource::IResource> dst,
                                  Ptr<const Resource::IResource> src) = 0;

        /// @brief Copies a byte range between two buffers.
        /// @param dst Destination buffer.
        /// @param dstOffset Byte offset in the destination.
        /// @param src Source buffer.
        /// @param srcOffset Byte offset in the source.
        /// @param size Number of bytes to copy.
        virtual void CopyBufferRegion(Ptr<Resource::IBuffer> dst, uint64_t dstOffset,
                                      Ptr<const Resource::IBuffer> src, uint64_t srcOffset,
                                      uint64_t size) = 0;

        /// @brief Copies a texel region between two textures.
        /// @param dst Destination texture.
        /// @param dstRegion Destination subresource and texel box.
        /// @param src Source texture.
        /// @param srcRegion Source subresource and texel box.
        virtual void CopyTextureRegion(Ptr<Resource::ITexture> dst,
                                       Link<const TextureRegion> dstRegion,
                                       Ptr<const Resource::ITexture> src,
                                       Link<const TextureRegion> srcRegion) = 0;

        /// @brief Uploads a CPU buffer into a texture subresource region.
        /// @param dst Destination texture.
        /// @param dstRegion Destination subresource and texel box.
        /// @param src Source buffer containing the texel data.
        /// @param srcOffset Byte offset of the first texel in the source.
        /// @param rowPitch Bytes between consecutive rows in the source; 0 = tightly packed.
        /// @param slicePitch Bytes between consecutive depth slices; 0 = tightly packed.
        virtual void CopyBufferToTexture(Ptr<Resource::ITexture> dst,
                                         Link<const TextureRegion> dstRegion,
                                         Ptr<const Resource::IBuffer> src, uint64_t srcOffset,
                                         uint32_t rowPitch, uint32_t slicePitch) = 0;

        /// @brief Reads a texture subresource region back into a CPU buffer.
        /// @param dst Destination buffer.
        /// @param dstOffset Byte offset of the first texel in the destination.
        /// @param rowPitch Bytes between consecutive rows in the destination; 0 = tightly packed.
        /// @param slicePitch Bytes between consecutive depth slices; 0 = tightly packed.
        /// @param src Source texture.
        /// @param srcRegion Source subresource and texel box.
        virtual void CopyTextureToBuffer(Ptr<Resource::IBuffer> dst, uint64_t dstOffset,
                                         uint32_t rowPitch, uint32_t slicePitch,
                                         Ptr<const Resource::ITexture> src,
                                         Link<const TextureRegion> srcRegion) = 0;

        /// @brief Resolves a multisampled subresource into a single-sampled one.
        /// @param dst Destination texture (sample count 1).
        /// @param dstSubresource Destination mip/slice.
        /// @param src Multisampled source texture.
        /// @param srcSubresource Source mip/slice.
        virtual void ResolveSubresource(Ptr<Resource::ITexture> dst,
                                        Link<const TextureSubresource> dstSubresource,
                                        Ptr<const Resource::ITexture> src,
                                        Link<const TextureSubresource> srcSubresource) = 0;
    };

    must_be_abstract(ICommandList)

    /// @brief Submits command lists to the GPU and synchronizes with fences.
    class API ICommandQueue // NOLINT
    {
    public:
        virtual ~ICommandQueue() = default;

        /// @brief Returns the device that created this queue.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;

        /// @brief Submits one command list for execution.
        /// @param cmd List in the closed state (Begin/End completed).
        virtual void Execute(Ptr<ICommandList> cmd) = 0;
        /// @brief Submits several command lists in order.
        /// @param cmds Lists in the closed state; submitted in span order.
        virtual void Execute(std::span<const Ptr<ICommandList>> cmds) = 0;

        /// @brief Enqueues a fence signal after previously submitted work.
        /// @param fence Fence to signal.
        /// @param value Value written when the work completes.
        virtual void Signal(Ptr<IFence> fence, uint64_t value) = 0;

        /// @brief Blocks the queue until the fence reaches @p value.
        /// @param fence Fence to wait on.
        /// @param value Minimum completed value required to continue.
        virtual void Wait(Ptr<IFence> fence, uint64_t value) = 0;

        /// @brief Submits all pending work and waits until the queue is idle.
        virtual void Flush() = 0;
    };

    must_be_abstract(ICommandQueue)

    /// @brief GPU timeline value used to synchronize CPU work with submission.
    ///
    /// A fence starts at a caller-chosen value and is advanced by Signal() on a
    /// queue. CompletedValue() reports the last value the GPU has reached, and
    /// Wait() blocks the CPU until a requested value is reached.
    class API IFence // NOLINT
    {
    public:
        virtual ~IFence() = default;

        /// @brief Returns the device that created this fence.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;

        /// @brief Returns the highest value the GPU has completed.
        /// @return Last completed fence value.
        [[nodiscard]] virtual uint64_t CompletedValue() const noexcept = 0;

        /// @brief Blocks the calling thread until the fence reaches @p value.
        /// @param value Value to wait for.
        virtual void Wait(uint64_t value) = 0;

        /// @brief Signals the fence from the CPU.
        /// @param value New completed value; must be greater than the current one.
        virtual void Signal(uint64_t value) = 0;
    };

    must_be_abstract(IFence)
}
