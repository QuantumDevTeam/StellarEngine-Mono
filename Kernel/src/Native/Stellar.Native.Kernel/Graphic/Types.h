// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Types.h
/// @brief Forward declarations and short descriptions of the public graphics types.
///
/// The header intentionally contains no definitions: it lets interfaces refer
/// to each other without pulling in heavy dependencies. Include the concrete
/// contract headers (or the Kernel/graphic_contracts.h umbrella) to use them.
#pragma once

/// @brief Rendering contracts: devices, commands, contexts, pipelines and resources.
namespace Stellar::Native::Kernel::Graphics
{
    /// @brief GPU device creation and resource factories.
    namespace Backend
    {
        /// @brief Owns the native GPU device and acts as a factory for contexts,
        /// queues, pipelines and resources.
        ///
        /// Created through Backend::MakeDevice<\B>(). Every Create*/Make* method
        /// returns null on failure; callers must check the result.
        class API IDevice;
    }

    /// @brief Command recording, submission and synchronization.
    namespace Command
    {
        /// @brief Records draw, dispatch, copy and state commands into a command buffer.
        ///
        /// A list is recorded between Begin() and End(), reset with Reset(), and
        /// submitted through ICommandQueue::Execute.
        class API ICommandList;

        /// @brief Submits command lists and signals/waits fences on the GPU timeline.
        class API ICommandQueue;

        /// @brief GPU timeline value used to synchronize CPU work with submission.
        class API IFence;

        /// @brief GPU query slot (occlusion, binary occlusion or timestamp).
        class API IQuery;
    }

    /// @brief Window-bound rendering context and presentation.
    namespace Context
    {
        /// @brief Binds a GPU device to a window and exposes its resolution,
        /// viewport and presentation queue.
        class API IContext;

        /// @brief Presents rendered images to a window and manages back buffers.
        class API ISwapChain;
    }

    /// @brief Pipelines and shader compilation.
    namespace Pipeline
    {
        /// @brief Immutable graphics or compute pipeline state.
        class API IPipelineState;

        /// @brief Compiles shader sources into backend shader blobs.
        class API IShaderCompiler;

        /// @brief Compiled shader stage and its bytecode.
        class API IShader;
    }

    /// @brief GPU resources and their CPU-side descriptions.
    namespace Resource
    {
        /// @brief Common base for GPU resources: native handle, state and debug name.
        class API IResource;

        /// @brief Linear GPU buffer (vertex, index, constant, structured, staging).
        class API IBuffer;

        /// @brief 1D, 2D, 3D or cube GPU texture with typed views.
        class API ITexture;

        /// @brief CRTP helpers shared by backend resource wrappers.
        namespace Core
        {
            /// @brief CRTP base for state, debug name and native handle forwarding.
            ///
            /// @tparam Derived Concrete resource type implementing NativeResource().
            /// @tparam TagT Handle tag that identifies the resource kind.
            template <typename Derived, typename TagT>
            class API ResourceCore;
        }
    }

    // exists

    /// @brief Rectangle in render-target space that maps NDC to pixels.
    ///
    /// X/Y are the top-left corner in pixels; Width/Height are extents;
    /// MinDepth/MaxDepth select the depth range (normally 0..1).
    struct API Viewport;

    /// @brief Linear RGBA color value with components typically in [0, 1].
    struct API Color4;

    /// @brief Mip level and array slice identifying one texture subresource.
    struct API TextureSubresource;

    /// @brief Subresource plus a texel-space box used by copy/upload operations.
    struct API TextureRegion;

    // COMMENT
    struct API BufferUploadDesc;

    // COMMENT
    struct API TextureUploadDesc;
}
