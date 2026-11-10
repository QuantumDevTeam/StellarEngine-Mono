// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Context/Descriptions.h
/// @brief Creation parameters for rendering contexts and swap chains.
#pragma once

#include "Graphic/Contracts/Core/Enums.h"

#include "Math/Types.h" // NOLINT

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Initial state of a rendering context.
    ///
    /// A context binds a device to a native window. The resolution and
    /// viewport are kept in sync when the context is created; changing one at
    /// runtime is done through IContext setters.
    struct API ContextDesc
    {
        Math::Vectors::Vector2<uint32_t> resolution{0, 0}; ///< Render resolution in pixels.
        Viewport viewport{
            0.0f,
            0.0f,
            static_cast<float>(resolution.X),
            static_cast<float>(resolution.Y)
        }; ///< Viewport covering the whole render resolution by default.

        /// @brief Creates a description with resolution and viewport already in sync.
        /// @param width Render width in pixels.
        /// @param height Render height in pixels.
        /// @return Description whose viewport covers the full resolution.
        [[nodiscard]] static constexpr ContextDesc WithResolution(uint32_t width, uint32_t height) noexcept
        {
            ContextDesc d;
            d.resolution = {width, height};
            d.viewport = Viewport{
                0.0f,
                0.0f,
                static_cast<float>(width),
                static_cast<float>(height)
            };
            return d;
        }
    };

    /// @brief Creation parameters for a window swap chain.
    ///
    /// Buffer count, format, sample count and presentation settings must be
    /// supported by the device; unsupported combinations make creation fail.
    struct API SwapChainDesc
    {
        // DXGI flip model limits; other backends map this to their own range.
        static constexpr uint32_t MinBufferCount = 2;  ///< Minimum number of back buffers.
        static constexpr uint32_t MaxBufferCount = 16; ///< Maximum number of back buffers.

        /// @brief Fullscreen presentation mode.
        ///
        /// A description can be windowed or exclusive fullscreen and carries
        /// the refresh rate, scaling and scanline order. Use Frameless() for
        /// borderless windowed fullscreen and Desktop() for exclusive mode.
        struct API FullscreenModeDesc
        {
            bool isWindowed = true; ///< True for windowed (including borderless) mode.
            uint32_t refreshNumerator = 0;   ///< Refresh-rate numerator in Hz (0 = current).
            uint32_t refreshDenominator = 1; ///< Refresh-rate denominator (usually 1).
            FullscreenScaling scaling = FullscreenScaling::Unspecified; ///< Image scaling mode.
            FullscreenScanlineOrder scanlineOrder = FullscreenScanlineOrder::Unspecified; ///< Interlaced scanline order.

            /// @brief Borderless windowed fullscreen at the requested refresh rate.
            /// @param refreshNumerator Refresh rate in Hz; 0 keeps the current rate.
            /// @return Windowed fullscreen description with unspecified scaling.
            [[nodiscard]] static constexpr FullscreenModeDesc Frameless(uint32_t refreshNumerator) noexcept
            {
                return FullscreenModeDesc(
                    true,
                    refreshNumerator,
                    1,
                    FullscreenScaling::Unspecified,
                    FullscreenScanlineOrder::Unspecified
                );
            }

            /// @brief Exclusive desktop fullscreen with centered scaling.
            /// @param refreshNumerator Refresh rate in Hz; 0 keeps the current rate.
            /// @return Exclusive fullscreen description with centered scaling.
            [[nodiscard]] static constexpr FullscreenModeDesc Desktop(uint32_t refreshNumerator) noexcept
            {
                return FullscreenModeDesc(
                    false,
                    refreshNumerator,
                    1,
                    FullscreenScaling::Centered,
                    FullscreenScanlineOrder::Progressive
                );
            }
        };

        SwapChainUsage usage = SwapChainUsage::RenderTargetOutput; ///< How the back buffers will be used.
        SwapChainFlags flags = SwapChainFlags::None;  ///< Optional presentation behavior.
        uint32_t bufferCount = 2;                     ///< Number of back buffers, clamped to [MinBufferCount, MaxBufferCount].
        Format bufferFormat = Format::RGBA8_UNorm;    ///< Back-buffer pixel format.

        bool stereo = false; ///< Request a stereo (two-eye) swap chain when supported.

        uint32_t sampleCount = 1;   ///< Multisample count per texel (1 = no MSAA).
        uint32_t sampleQuality = 0; ///< Backend-specific quality level within sampleCount.

        ScalingType scaling = ScalingType::Stretch;      ///< How the image is scaled to the output.
        SwapEffect swapEffect = SwapEffect::FlipDiscard;     ///< Presentation model; flip effects are preferred.
        AlphaMode alphaMode = AlphaMode::Unspecified;    ///< Alpha interpretation of the presented image.

        ColorSpace colorSpace = ColorSpace::SRGB_NonLinear; ///< Color space and transfer function.

        // 0 = present immediately, 1 = sync to every refresh, N = sync to N-th refresh.
        uint32_t vsyncInterval = 1;  ///< Vertical-sync interval; 0 disables synchronization.
        uint32_t maxFrameLatency = 1;///< Maximum queued frames for latency control (backend-dependent).

        FullscreenModeDesc fullscreenMode = FullscreenModeDesc::Frameless(0); ///< Fullscreen settings (windowed by default).
    };
}
