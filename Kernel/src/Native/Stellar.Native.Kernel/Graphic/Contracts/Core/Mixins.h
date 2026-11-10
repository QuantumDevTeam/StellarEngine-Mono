// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Core/Mixins.h
/// @brief CRTP mixins that add common resource operations to backend wrappers.
#pragma once

#include <cstdint>

#include "Graphic/Contracts/Core/Enums.h"

/// @brief Reusable CRTP mixins for backend resource wrappers.
namespace Stellar::Native::Kernel::Graphics::Resource::Mixins
{
    /// @brief Adds Map/Unmap to a resource by forwarding to Derived::MapImpl/UnmapImpl.
    /// @tparam Derived Concrete resource wrapper implementing both Impl methods.
    template <typename Derived>
    class API MappableMixin
    {
    public:
        /// @brief Maps the resource for CPU access.
        /// @param mode Intended access pattern and synchronization hint.
        /// @return Pointer to the start of the mapped range.
        /// @note The mapping stays valid until Unmap() is called.
        void* Map(MapMode mode = MapMode::WriteDiscard) { return static_cast<Derived*>(this)->MapImpl(mode); }

        /// @brief Releases the mapping and makes CPU writes visible to the GPU.
        void Unmap() { static_cast<Derived*>(this)->UnmapImpl(); }
    };

    /// @brief Adds a one-shot byte upload to a mappable resource.
    /// @tparam Derived Concrete resource wrapper deriving from MappableMixin.
    template <typename Derived>
    class API UploadMixin
    {
    public:
        /// @brief Copies a byte range into the mapped resource.
        /// @param data data upload description.
        /// @note The resource must be mappable; no bounds checking is performed.
        void Upload(Link<const BufferUploadDesc> data)
        {
            // auto* self = static_cast<Derived*>(this);
            // void* mapped = self->MapImpl();
            // std::memcpy(static_cast<std::byte*>(mapped) + data.dstOffset,
            //             data.bytes.data(), data.bytes.size());
            // self->UnmapImpl();
            static_cast<Derived*>(this)->UploadImpl(data);
        }
    };

    /// @brief Adds getSize() by forwarding to Derived::ByteSize().
    /// @tparam Derived Concrete resource wrapper.
    template <typename Derived>
    class API SizedMixin
    {
    public:
        /// @brief Returns the resource size in bytes.
        /// @return Size in bytes.
        size_t getSize() const noexcept { return static_cast<const Derived*>(this)->ByteSize(); }
    };

    /// @brief Adds element stride reporting on top of SizedMixin.
    /// @tparam Derived Concrete buffer wrapper implementing ByteStride().
    template <typename Derived>
    class API SizedAndStructuredMixin
            : public SizedMixin<Derived>
    {
    public:
        /// @brief Returns the distance between consecutive elements in bytes.
        /// @return Stride in bytes.
        size_t getStride() const noexcept { return static_cast<const Derived*>(this)->ByteStride(); }
    };

    /// @brief Exposes texture dimensions through Derived::getDesc().
    /// @tparam Derived Concrete texture wrapper.
    template <typename Derived>
    class API DimensionedMixin
    {
    public:
        /// @brief Texture dimensionality (1D, 2D, 3D or cube).
        /// @return Texture type enum value.
        TextureType getType() const noexcept { return static_cast<const Derived*>(this)->getDesc().type; }
        /// @brief Width in texels of mip level 0.
        /// @return Width in texels.
        uint32_t getWidth() const noexcept { return static_cast<const Derived*>(this)->getDesc().width; }
        /// @brief Height in texels of mip level 0.
        /// @return Height in texels.
        uint32_t getHeight() const noexcept { return static_cast<const Derived*>(this)->getDesc().height; }
        /// @brief Depth in texels of mip level 0 (1 for 1D/2D textures).
        /// @return Depth in texels.
        uint32_t getDepth() const noexcept { return static_cast<const Derived*>(this)->getDesc().depth; }
        /// @brief Number of mip levels, including the base level.
        /// @return Number of mip levels.
        uint32_t getMipLevels() const noexcept { return static_cast<const Derived*>(this)->getDesc().mipLevels; }
        /// @brief Number of array slices (six per cube map).
        /// @return Number of array slices.
        uint32_t getArraySize() const noexcept { return static_cast<const Derived*>(this)->getDesc().arraySize; }
        /// @brief Sample count per texel (1 for non-multisampled textures).
        /// @return Sample count.
        uint32_t getSampleCount() const noexcept { return static_cast<const Derived*>(this)->getDesc().sampleCount; }
    };

    /// @brief Adds generic binding to a command list via Derived::BindImpl.
    /// @tparam Derived Concrete resource wrapper.
    template <typename Derived>
    class API BindableMixin
    {
    public:
        /// @brief Binds the resource to a command list.
        /// @tparam CmdT Command-list type accepted by the backend.
        /// @param cmd Command list that will receive the binding.
        /// @param slot Register/descriptor slot to bind to.
        template <typename CmdT>
        void Bind(CmdT& cmd, uint32_t slot)
        {
            static_cast<Derived*>(this)->BindImpl(cmd, slot);
        }
    };

    /// @brief Adds resource-to-resource copy via Derived::CopyToImpl.
    /// @tparam Derived Concrete resource wrapper.
    template <typename Derived>
    class API CopyableMixin
    {
    public:
        /// @brief Copies the contents of this resource into @p dst.
        /// @param dst Destination resource of the same concrete type.
        void CopyTo(Derived& dst)
        {
            static_cast<Derived*>(this)->CopyToImpl(dst);
        }
    };
}
