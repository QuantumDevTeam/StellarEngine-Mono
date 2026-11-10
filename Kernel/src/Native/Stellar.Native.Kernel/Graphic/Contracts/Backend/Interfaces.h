// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Backend/Interfaces.h
/// @brief GPU device interface and adapter information.
#pragma once

#include "Graphic/Types.h" // NOLINT
#include "Graphic/Contracts/Core/Enums.h"
#include "Graphic/Contracts/Context/Descriptions.h"
#include "Graphic/Contracts/Pipeline/Descriptions.h"
#include "Graphic/Contracts/Resource/Descriptions.h"

#include "Platform/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Graphics::Backend
{
    /// @brief Properties of the physical adapter used by a device.
    struct API AdapterInfo
    {
        std::string_view name{}; ///< Adapter name reported by the driver (valid for the device lifetime).
        uint32_t vendorId = 0;   ///< PCI vendor identifier.
        uint32_t deviceId = 0;   ///< PCI device identifier assigned by the vendor.

        uint64_t dedicatedMemory = 0; ///< Dedicated video memory in bytes, or 0 when unknown.
        uint64_t sharedMemory = 0;    ///< Shared system memory available to the adapter, in bytes.

        uint64_t timestampFrequency = 0; ///< Timestamp ticks per second; 0 when timestamps are unsupported.
    };

    /// @brief Owns the native GPU device and creates every other GPU object.
    ///
    /// A device is created through Backend::MakeDevice<\B>() and is shared with
    /// all objects it creates. Every Create*/Make* method returns null on
    /// failure. The backend logs the reason (validation, unsupported format,
    /// out of memory, ...); there is no separate error channel yet, so callers
    /// must check the result.
    class API IDevice // NOLINT
    {
    public:
        virtual ~IDevice() = default;

        /// @brief Creates a rendering context bound to a native window.
        /// @param window Window that will receive presented frames. The context
        ///        keeps only a non-owning reference; the window must outlive it.
        /// @param desc Initial resolution, viewport and presentation settings.
        /// @return New context, or null when creation fails.
        [[nodiscard]] virtual Ptr<Context::IContext> MakeContext(
            Ptr<const Platform::Window::IWindow> window,
            Link<const ContextDesc> desc) = 0;

        /// @brief Creates a command queue for the requested command family.
        /// @param usage Command family the queue will execute; Direct by default.
        /// @return New queue, or null when creation fails.
        [[nodiscard]] virtual Ptr<Command::ICommandQueue> CreateCommandQueue(
            CommandListUsage usage = CommandListUsage::Direct) = 0;

        /// @brief Creates a command list associated with a queue.
        /// @param queue Queue that will execute the list; may be null when the
        ///        backend allows recording without a queue affinity.
        /// @return New command list, or null when creation fails.
        [[nodiscard]] virtual Ptr<Command::ICommandList> CreateCommandList(
            Ptr<Command::ICommandQueue> queue) = 0;

        /// @brief Creates a GPU fence.
        /// @param initial Initial completed value.
        /// @return New fence, or null when creation fails.
        [[nodiscard]] virtual Ptr<Command::IFence> CreateFence(uint64_t initial = 0) = 0;

        /// @brief Creates a query object of the requested type.
        /// @param type Query kind (occlusion, binary occlusion or timestamp).
        /// @return New query, or null when the type is unsupported.
        [[nodiscard]] virtual Ptr<Command::IQuery> CreateQuery(QueryType type) = 0;

        /// @brief Creates a graphics pipeline state.
        /// @param desc Shaders, input layout, rasterizer/blend/depth state and formats.
        /// @return New pipeline, or null when validation or compilation fails.
        [[nodiscard]] virtual Ptr<Pipeline::IPipelineState> CreatePipeline(
            Link<const PipelineDesc> desc) = 0;

        /// @brief Creates a compute pipeline state.
        /// @param desc Compute shader and binding layout.
        /// @return New pipeline, or null when validation or compilation fails.
        [[nodiscard]] virtual Ptr<Pipeline::IPipelineState> CreateComputePipeline(
            Link<const ComputePipelineDesc> desc) = 0;

        // A null result means creation failed; the backend is expected to log
        // the reason through the device's diagnostics channel.
        /// @brief Creates a GPU buffer.
        /// @param desc Size, usage, flags and initial state.
        /// @return New buffer, or null when creation fails.
        [[nodiscard]] virtual Ptr<Resource::IBuffer> CreateBuffer(Link<const BufferDesc> desc) = 0;

        /// @brief Creates a GPU texture.
        /// @param desc Dimensions, format, usage and binding flags.
        /// @return New texture, or null when creation fails.
        [[nodiscard]] virtual Ptr<Resource::ITexture> CreateTexture(Link<const TextureDesc> desc) = 0;

        // The swap chain keeps the context alive for as long as it is presented.
        /// @brief Creates a swap chain for a context.
        /// @param context Context whose window receives the presented frames.
        /// @param desc Back-buffer count, format, presentation and fullscreen settings.
        /// @return New swap chain, or null when the configuration is unsupported.
        [[nodiscard]] virtual Ptr<Context::ISwapChain> CreateSwapChain(
            Ptr<Context::IContext> context,
            Link<const SwapChainDesc> desc) = 0;

        /// @brief Human-readable backend name (for example "DirectX 12").
        /// @return Backend name string.
        [[nodiscard]] virtual std::string_view getBackendName() const noexcept = 0;

        /// @brief Opaque native device handle (ID3D12Device*, VkDevice, ...).
        /// @note The type is backend-defined; use it only for interop.
        /// @return Pointer to the native device object.
        [[nodiscard]] virtual void* getNativeHandle() const noexcept = 0;

        /// @brief Returns information about the selected physical adapter.
        /// @return Reference to the adapter info structure.
        [[nodiscard]] virtual Link<const AdapterInfo> getAdapterInfo() const noexcept = 0;

        /// @brief Checks whether a format can be used with a texture type and binding flags.
        /// @param format Pixel format to validate.
        /// @param type Texture dimensionality the format will be used with.
        /// @param usage Capabilities the texture will require.
        /// @return True when the combination is supported by this device.
        [[nodiscard]] virtual bool IsFormatSupported(Format format, TextureType type,
                                                     TextureFlags usage) const noexcept = 0;
    };

    /// @brief Compares two devices by backend name and native handle.
    /// @param a First device.
    /// @param b Second device.
    /// @return True when both devices belong to the same backend and wrap the same native handle.
    [[nodiscard]] constexpr bool operator==(Link<const IDevice> a, Link<const IDevice> b) noexcept
    {
        return a.getBackendName() == b.getBackendName()
               && a.getNativeHandle() == b.getNativeHandle();
    }

    must_be_abstract(IDevice)
}
