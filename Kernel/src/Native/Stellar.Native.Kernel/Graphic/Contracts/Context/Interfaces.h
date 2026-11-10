// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Context/Interfaces.h
/// @brief Rendering context and swap-chain interfaces.
#pragma once

#include <string>

#include "Graphic/Types.h" // NOLINT
#include "Graphic/Contracts/Core/Concepts.h"

#include "Math/Types.h" // NOLINT
#include "Platform/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Graphics::Context
{
    namespace Window = Stellar::Native::Kernel::Platform::Window;

    /// @brief Binds a GPU device to a native window and owns its presentation state.
    ///
    /// A context is created by Backend::IDevice::MakeContext and exposes the
    /// resolution and viewport used by the renderer. Swap chains are created
    /// from the same device and keep the context alive while presenting.
    class API IContext // NOLINT
    {
    public:
        virtual ~IContext() = default;

        /// @brief Returns the device that created this context.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;
        [[nodiscard]] virtual Ptr<const Window::IWindow> getWindow() const noexcept = 0;

        /// @brief Returns the current render resolution in pixels.
        /// @return Reference to the resolution vector.
        [[nodiscard]] virtual Link<const Math::Vectors::Vector2<uint32_t>> getResolution() const noexcept = 0;
        /// @brief Returns the current viewport.
        /// @return Reference to the viewport.
        [[nodiscard]] virtual Link<const Viewport> getViewport() const noexcept = 0;

        // Link parameters are read and copied during the call; the context
        // never stores a reference to caller-owned data.
        /// @brief Changes the render resolution.
        /// @param res New size in pixels; must be non-zero.
        virtual void setResolution(Link<const Math::Vectors::Vector2<uint32_t>> res) = 0;

        /// @brief Changes the viewport without changing the resolution.
        /// @param viewport New viewport rectangle.
        virtual void setViewport(Link<const Viewport> viewport) = 0;

        /// @brief Blocks until all GPU work submitted for this context has completed.
        virtual void WaitIdle() = 0;

        /// @brief Returns the debug name used by validation and profiling tools.
        /// @return Debug name string view.
        [[nodiscard]] virtual std::string_view getDebugName() const noexcept = 0;
        /// @brief Sets the debug name used by validation and profiling tools.
        /// @param name New name; copied into the context.
        virtual void setDebugName(std::string name) = 0;
    };

    must_be_abstract(IContext)
    static_assert(Concepts::HasDebugName<IContext>, "IContext must provide debug name");

    /// @brief Presents rendered images to a window and manages its back buffers.
    ///
    /// The swap chain owns the back-buffer textures. Call Present() once per
    /// frame after recording and submitting the frame commands. Resize() must
    /// be called when the window size changes; it recreates the buffers and
    /// invalidates previously returned back-buffer pointers.
    class API ISwapChain // NOLINT
    {
    public:
        virtual ~ISwapChain() = default;

        /// @brief Returns the device that created this swap chain.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;
        /// @brief Returns the context this swap chain presents for.
        /// @return Shared pointer to the context.
        [[nodiscard]] virtual Ptr<IContext> getContext() const noexcept = 0;

        // The swap chain owns its back buffers; callers only observe them.
        /// @brief Returns a back-buffer texture by index.
        /// @param index Back-buffer index in [0, bufferCount).
        /// @return Non-owning pointer to the texture, valid until the next Resize().
        [[nodiscard]] virtual Ptr<Resource::ITexture> getBackBuffer(uint32_t index) = 0;
        /// @brief Index of the buffer that will be presented next.
        /// @return Current back-buffer index.
        [[nodiscard]] virtual uint32_t getCurrentBackBufferIndex() const noexcept = 0;

        /// @brief Current vertical-sync interval (0 = immediate presentation).
        /// @return VSync interval in frames.
        [[nodiscard]] virtual uint32_t getVSyncInterval() const noexcept = 0;
        /// @brief Changes the vertical-sync interval for subsequent Present calls.
        /// @param interval 0 = immediate, 1 = every refresh, N = every N-th refresh.
        virtual void setVSyncInterval(uint32_t interval) = 0;

        /// @brief Presents the current back buffer to the window.
        /// @param flags Optional presentation modifiers.
        virtual void Present(PresentFlags flags = PresentFlags::None) = 0;

        /// @brief Recreates the back buffers at a new size.
        /// @param size New size in pixels; must be non-zero.
        /// @note All previously returned back-buffer pointers become invalid.
        virtual void Resize(Math::Vectors::Vector2<uint32_t> size) = 0;
    };

    must_be_abstract(ISwapChain)
}
