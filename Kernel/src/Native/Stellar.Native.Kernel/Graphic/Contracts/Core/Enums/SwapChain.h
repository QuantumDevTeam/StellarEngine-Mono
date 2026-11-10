// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/SwapChain.h
/// @brief Presentation model, fullscreen, color-space and scaling enumerations.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief How presented frames relate to the back-buffer contents.
    ///
    /// Flip-model effects are preferred on modern APIs: they let the
    /// compositor present without an extra copy. Sequential/Discard are
    /// legacy blit-model effects kept for compatibility.
    enum class API SwapEffect : uint32_t
    {
        Discard        = 0,   ///< Legacy blit model; buffer contents are undefined after Present.
        Sequential     = 1,   ///< Legacy blit model; buffer contents are preserved after Present.
        FlipSequential = 3,   ///< Flip model; buffer contents are preserved after Present.
        FlipDiscard    = 4,   ///< Flip model; buffer contents are undefined after Present.
    };

    /// @brief Image scaling used when presenting in fullscreen mode.
    enum class API FullscreenScaling : uint32_t
    {
        Unspecified = 0,    ///< Let the backend/driver choose.
        Centered    = 1,    ///< Center the image without scaling.
        Stretched   = 2,    ///< Stretch the image to fill the display.
    };

    /// @brief Scanline order for interlaced fullscreen presentation.
    enum class API FullscreenScanlineOrder : uint32_t
    {
        Unspecified     = 0,     ///< Let the backend/driver choose.
        Progressive     = 1,     ///< Progressive (non-interlaced) output.
        UpperFieldFirst = 2,     ///< Interlaced, upper field first.
        LowerFieldFirst = 3,     ///< Interlaced, lower field first.
    };

    /// @brief Declares how a swap-chain image may be used.
    enum class API SwapChainUsage : uint32_t
    {
        None               = 0,           ///< No usage declared.
        BackBuffer         = 1u << 0,     ///< Image is a back buffer.
        DiscardOnPresent   = 1u << 1,     ///< Contents may be discarded after Present.
        ReadOnly           = 1u << 2,     ///< Image is read-only.
        RenderTargetOutput = 1u << 3,     ///< Image can be bound as a render target.
        ShaderInput        = 1u << 4,     ///< Image can be sampled by shaders.
        UnorderedAccess    = 1u << 5,     ///< Image can be bound as a UAV.
        Shared             = 1u << 6,     ///< Image can be shared with another API/process.
    };

    /// @brief Optional swap-chain behavior flags.
    enum class API SwapChainFlags : uint32_t
    {
        None                 = 0,         ///< No optional behavior.
        AllowTearing         = 1u << 0,   ///< Allow variable refresh / tearing when supported.
        AllowModeSwitch      = 1u << 1,   ///< Allow exclusive fullscreen mode switches.
        FrameLatencyWaitable = 1u << 2,   ///< Expose a waitable object for frame-latency control.
    };

    /// @brief Modifiers applied to a single Present call.
    enum class API PresentFlags : uint32_t
    {
        None      = 0,         ///< Normal presentation, synchronized with vsync.
        DoNotWait = 1u << 0,   ///< Do not block on the previous frame.
        Test      = 1u << 1,   ///< Validate presentation without displaying the frame.
        Restart   = 1u << 2,   ///< Restart the presentation sequence after a mode change.
    };

    /// @brief Color space and transfer function of the swap-chain image.
    enum class API ColorSpace : uint32_t
    {
        Unspecified = 0,       ///< Let the backend/driver choose.
        SRGB_NonLinear,        ///< Standard sRGB, non-linear encoding.
        DisplayP3_NonLinear,   ///< Display-P3 primaries with sRGB transfer.
        HDR10_ST2084,          ///< HDR10 with SMPTE ST 2084 (PQ) transfer.
        HDR10_HLG,             ///< HDR10 with hybrid log-gamma transfer.
        DolbyVision,           ///< Dolby Vision metadata/pipeline.
    };

    /// @brief How the swap-chain image is scaled to the output.
    enum class API ScalingType : uint32_t
    {
        None               = 0,  ///< No scaling.
        AspectRatioStretch = 1,  ///< Stretch while preserving the aspect ratio.
        Stretch            = 2,  ///< Stretch to fill the output, ignoring aspect ratio.
    };

    /// @brief Alpha interpretation of the presented image.
    enum class API AlphaMode : uint32_t
    {
        Unspecified   = 0,  ///< Let the backend/driver choose.
        Force         = 1,  ///< Force alpha compositing.
        Ignore        = 2,  ///< Ignore the alpha channel.
        Premultiplied = 3,  ///< RGB is premultiplied by alpha.
        Straight      = 4,  ///< Alpha is straight (not premultiplied).
    };
}

namespace Stellar::Native // opt-in flags
{
    /// @brief Enables bitwise flag operators for Graphics::SwapChainUsage.
    template <> struct IsFlagEnum<SwapChainUsage> : std::true_type {};

    /// @brief Enables bitwise flag operators for Graphics::SwapChainFlags.
    template <> struct IsFlagEnum<SwapChainFlags> : std::true_type {};

    /// @brief Enables bitwise flag operators for Graphics::PresentFlags.
    template <> struct IsFlagEnum<PresentFlags> : std::true_type {};
}
