// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Resource/Interfaces.h
/// @brief Common resource, buffer and texture interfaces.
#pragma once

#include <string>

#include "Graphic/Types.h" // NOLINT
#include "Graphic/Contracts/Core/Enums.h"
#include "Graphic/Contracts/Core/Format.h"
#include "Graphic/Contracts/Core/Concepts.h"

namespace Stellar::Native::Kernel::Graphics::Resource
{
    /// @brief Common base for every GPU resource.
    ///
    /// A resource exposes its device, native handle, current usage state and a
    /// debug name. The state is used by validation and by Transition() calls;
    /// it is not updated automatically when commands use the resource.
    class API IResource // NOLINT
    {
    public:
        virtual ~IResource() = default;

        /// @brief Returns the device that created this resource.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;

        // Native handles and mapped memory stay raw: they are platform API
        // values (ID3D11Resource*, VkImage, GLuint, ...), not engine-owned
        // objects, so wrapping them in Handles::Handle would only add a layer.
        /// @brief Opaque native resource handle.
        /// @note The type is backend-defined; use it only for interop.
        /// @return Pointer to the native resource object.
        [[nodiscard]] virtual void* getNativeHandle() const noexcept = 0;

        /// @brief Current usage state recorded by the caller.
        /// @return Current resource state.
        [[nodiscard]] virtual ResourceState getState() const noexcept = 0;
        /// @brief Updates the recorded usage state without emitting a barrier.
        /// @param s New state to record.
        virtual void setState(ResourceState s) noexcept = 0;

        /// @brief Returns the debug name used by validation and profiling tools.
        /// @return Debug name string view.
        [[nodiscard]] virtual std::string_view getDebugName() const noexcept = 0;
        /// @brief Sets the debug name used by validation and profiling tools.
        /// @param name New name; copied into the resource.
        virtual void setDebugName(std::string name) = 0;
    };

    must_be_abstract(IResource)
    must_provide_native_handle(IResource)
    static_assert(Concepts::GpuResource<IResource>, "IResource must be a GPU resource");

    /// @brief Linear GPU buffer.
    ///
    /// Buffers are typed by their description flags: vertex, index, constant,
    /// structured or raw. Mappable buffers support direct CPU access; all
    /// buffers support the convenience Upload() path when the backend can
    /// stage the data.
    class API IBuffer : public IResource // NOLINT
    {
    public:
        /// @brief Total buffer size in bytes.
        /// @return Size in bytes.
        [[nodiscard]] virtual uint64_t getSize() const noexcept = 0;
        /// @brief Distance between consecutive elements in bytes; 0 for raw buffers.
        /// @return Stride in bytes.
        [[nodiscard]] virtual uint32_t getStride() const noexcept = 0;

        /// @brief Maps the buffer for CPU access.
        /// @param mode Intended access pattern; WriteDiscard is the default fast path.
        /// @return Pointer to the start of the mapped range.
        /// @note The mapping stays valid until Unmap() is called.
        virtual void* Map(MapMode mode = MapMode::WriteDiscard) = 0;

        /// @brief Releases the mapping and makes CPU writes visible to the GPU.
        virtual void Unmap() = 0;

        /// @brief Copies a byte range into the buffer.
        /// @param data Bytes to copy.
        /// @note Implementations may map internally or use a staging copy.
        virtual void Upload(Link<const BufferUploadDesc> data) = 0;
    };

    must_be_abstract(IBuffer)
    static_assert(Concepts::Mappable<IBuffer>, "IBuffer must be Mappable");
    static_assert(Concepts::Uploadable<IBuffer, BufferUploadDesc>, "IBuffer must be Uploadable");

    /// @brief 1D, 2D, 3D or cube GPU texture.
    ///
    /// A texture owns its pixel storage and one view per supported usage
    /// (render target, depth/stencil, shader resource, unordered access).
    /// Views are created eagerly and returned as opaque native handles.
    class API ITexture : public IResource // NOLINT
    {
    public:
        /// @brief Texture dimensionality.
        /// @return Texture type enum value.
        [[nodiscard]] virtual TextureType getType() const noexcept = 0;
        /// @brief Width in texels of mip level 0.
        /// @return Width in texels.
        [[nodiscard]] virtual uint32_t getWidth() const noexcept = 0;
        /// @brief Height in texels of mip level 0.
        /// @return Height in texels.
        [[nodiscard]] virtual uint32_t getHeight() const noexcept = 0;
        /// @brief Depth in texels of mip level 0 (1 for 1D/2D textures).
        /// @return Depth in texels.
        [[nodiscard]] virtual uint32_t getDepth() const noexcept = 0;
        /// @brief Number of mip levels, including the base level.
        /// @return Number of mip levels.
        [[nodiscard]] virtual uint32_t getMipLevels() const noexcept = 0;
        /// @brief Number of array slices (six per cube map).
        /// @return Number of array slices.
        [[nodiscard]] virtual uint32_t getArraySize() const noexcept = 0;
        /// @brief Samples per texel (1 for non-multisampled textures).
        /// @return Sample count.
        [[nodiscard]] virtual uint32_t getSampleCount() const noexcept = 0;
        /// @brief Pixel format of the texture.
        /// @return Pixel format enum value.
        [[nodiscard]] virtual Format getPixelFormat() const noexcept = 0;

        // Copies CPU data into one subresource region. rowPitch/slicePitch may
        // be 0 when the data is tightly packed. The implementation is free to
        // stage internally, so this is valid for non-mappable textures too.
        /// @brief Copies CPU data into one subresource region.
        /// @param data Source bytes; layout is described by rowPitch/slicePitch.
        virtual void Upload(Link<const TextureUploadDesc> data) = 0;

        // Opaque view/descriptor handles. Their content is backend-defined:
        // ID3D11View*, a DX12 CPU descriptor handle, a GLuint, ...
        /// @brief Opaque render-target view handle, or null when unsupported.
        /// @return Native RTV handle, or null.
        [[nodiscard]] virtual void* RTV() const noexcept = 0;
        /// @brief Opaque depth/stencil view handle, or null when unsupported.
        /// @return Native DSV handle, or null.
        [[nodiscard]] virtual void* DSV() const noexcept = 0;
        /// @brief Opaque shader-resource view handle, or null when unsupported.
        /// @return Native SRV handle, or null.
        [[nodiscard]] virtual void* SRV() const noexcept = 0;
        /// @brief Opaque unordered-access view handle, or null when unsupported.
        /// @return Native UAV handle, or null.
        [[nodiscard]] virtual void* UAV() const noexcept = 0;
    };

    must_be_abstract(ITexture)
    static_assert(Concepts::Uploadable<ITexture, TextureUploadDesc>, "ITexture must be Uploadable");
}
