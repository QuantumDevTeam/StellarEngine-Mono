// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Core/Concepts.h
/// @brief Concepts used to validate graphics backends and resource wrappers.
#pragma once

#include <concepts>
#include <cstdint>
#include <string_view>
#include <type_traits>

#include "Graphic/Types.h" // NOLINT
#include "Graphic/Contracts/Core/Enums.h"

/// @brief Validation concepts for graphics backends and resources.
namespace Stellar::Native::Kernel::Graphics::Concepts
{
    /// @brief A type that exposes a native GPU handle and a resource state.
    ///
    /// Satisfied by Resource::IResource and by backend resource wrappers that
    /// follow the same interface.
    template <typename R>
    concept GpuResource = requires(const R& r)
    {
        { r.getNativeHandle() } -> std::convertible_to<void*>;
        { r.getState() } -> std::convertible_to<ResourceState>;
    };

    /// @brief A type that exposes its backend-native resource for interop.
    template <typename D>
    concept ProvidesNativeResource = requires(const D& d)
    {
        { d.getNativeResource() } -> std::convertible_to<void*>;
    };

    /// @brief A resource that can be mapped into CPU address space.
    template <typename R>
    concept Mappable = requires(R& r, MapMode mode)
    {
        { r.Map(mode) } -> std::convertible_to<void*>;
        { r.Unmap() } -> std::same_as<void>;
    };

    /// @brief A resource that can receive a byte range.
    ///
    /// Desc is a transfer record, not the buffer upload signature.
    template <typename R, typename DescT>
    concept Uploadable = requires(R& r, const DescT& data)
    {
        { r.Upload(data) } -> std::same_as<void>;
    };

    /// @brief A resource that reports its byte size.
    template <typename R>
    concept Sized = requires(const R& r)
    {
        { r.getSize() } -> std::convertible_to<size_t>;
    };

    /// @brief A buffer that can be bound as a constant buffer on a command list.
    template <typename R, typename CmdT>
    concept BindableAsCBV = requires(R& r, CmdT& cmd, uint32_t slot)
    {
        { r.BindAsCBV(cmd, slot) } -> std::same_as<void>;
    };

    template <typename O>
    concept HasDebugName = requires(O& o, std::string name)
    {
        { o.getDebugName() } -> std::convertible_to<std::string_view>;
        { o.setDebugName(name) } -> std::same_as<void>;
    };

    /// @brief A complete graphics backend descriptor.
    ///
    /// A backend must provide native wrapper types for every major GPU object,
    /// a stable display name and a `Create` function returning the device
    /// interface. The wrapper types must derive from the corresponding kernel
    /// interfaces so callers can use them polymorphically.
    template <typename B>
    concept Backend = requires(DeviceFlags flags)
    {
        typename B::CommandList;
        typename B::CommandQueue;
        typename B::Fence;

        typename B::Context;
        typename B::SwapChain;

        typename B::Pipeline;

        typename B::Buffer;
        typename B::Texture;

        { B::getName() } -> std::convertible_to<std::string_view>;
        { B::Create(flags) } -> std::convertible_to<Ptr<Backend::IDevice>>;

        requires std::derived_from<typename B::CommandList, Command::ICommandList>;
        requires std::derived_from<typename B::CommandQueue, Command::ICommandQueue>;
        requires std::derived_from<typename B::Fence, Command::IFence>;

        requires std::derived_from<typename B::Context, Context::IContext>;
        requires std::derived_from<typename B::SwapChain, Context::ISwapChain>;

        requires std::derived_from<typename B::Pipeline, Pipeline::IPipelineState>;

        requires std::derived_from<typename B::Buffer, Resource::IBuffer>;
        requires std::derived_from<typename B::Texture, Resource::ITexture>;
    };
}
