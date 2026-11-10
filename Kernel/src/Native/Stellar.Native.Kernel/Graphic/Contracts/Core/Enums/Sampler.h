// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/Sampler.h
/// @brief Texture addressing, filtering and border-color enumerations.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Behavior when a texture coordinate falls outside [0, 1].
    enum class API TextureAddressMode : uint32_t
    {
        Wrap,       ///< Repeat the texture (fract coordinate).
        Mirror,     ///< Mirror the texture at integer boundaries.
        Clamp,      ///< Clamp to the edge texel.
        Border,     ///< Return the configured border color.
        MirrorOnce, ///< Mirror once, then clamp to the edge.
    };

    /// @brief Minification, magnification and mip filtering combination.
    ///
    /// The name lists the filter used for minification, magnification and mip
    /// selection respectively. `Anisotropic` lets the backend choose the
    /// sample count from SamplerDesc::maxAnisotropy. `Comparison*` filters
    /// return a comparison result and are used for shadow maps.
    enum class API Filter : uint32_t
    {
        MinMagMipPoint,                ///< Point filtering for all three stages.
        MinMagPointMipLinear,          ///< Point min/mag, linear mip interpolation.
        MinPointMagLinearMipPoint,     ///< Point min, linear mag, point mip.
        MinPointMagMipLinear,          ///< Point min, linear mag/mip.
        MinLinearMagMipPoint,          ///< Linear min, point mag/mip.
        MinLinearMagPointMipLinear,    ///< Linear min, point mag, linear mip.
        MinMagLinearMipPoint,          ///< Linear min/mag, point mip.
        MinMagMipLinear,               ///< Trilinear filtering (default).
        Anisotropic,                   ///< Anisotropic filtering.
        ComparisonMinMagMipPoint,      ///< Comparison filter with point filtering.
        ComparisonMinMagMipLinear,     ///< Comparison filter with linear filtering.
        ComparisonAnisotropic,         ///< Comparison filter with anisotropic filtering.
        MinimumMinMagMipPoint,         ///< Minimum reduction filter, point filtering.
        MinimumMinMagMipLinear,        ///< Minimum reduction filter, linear filtering.
        MaximumMinMagMipPoint,         ///< Maximum reduction filter, point filtering.
        MaximumMinMagMipLinear,        ///< Maximum reduction filter, linear filtering.
    };

    /// @brief Color returned by TextureAddressMode::Border outside the texture.
    enum class API BorderColor : uint32_t
    {
        TransparentBlack, ///< (0, 0, 0, 0).
        OpaqueBlack,      ///< (0, 0, 0, 1).
        OpaqueWhite,      ///< (1, 1, 1, 1).
    };
}
