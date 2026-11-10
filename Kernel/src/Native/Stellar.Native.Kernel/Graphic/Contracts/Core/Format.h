// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Core/Format.h
/// @brief Portable pixel formats and their size/block traits.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Portable pixel format identifier.
    ///
    /// Channels are listed in memory order, the suffix describes the component
    /// type, and an `sRGB` suffix marks a non-linear sRGB view of the storage.
    /// BC* formats are block-compressed and addressed in 4x4 texel blocks.
    ///
    /// @note Not every backend supports every format; query
    /// Backend::IDevice::IsFormatSupported before creating a resource.
    enum class API Format : uint32_t
    {
        Unknown = 0, ///< Unset/invalid format.

        // 8
        R8_UNorm, ///< 8-bit single-channel unsigned normalized.
        R8_SNorm, ///< 8-bit single-channel signed normalized.
        R8_UInt,  ///< 8-bit single-channel unsigned integer.
        R8_SInt,  ///< 8-bit single-channel signed integer.

        // 8+8
        RG8_UNorm, ///< 8-bit two-channel unsigned normalized.
        RG8_SNorm, ///< 8-bit two-channel signed normalized.
        RG8_UInt,  ///< 8-bit two-channel unsigned integer.
        RG8_SInt,  ///< 8-bit two-channel signed integer.

        // 4+4+4+4
        BGRA4_UNorm, ///< 16-bit packed BGRA, 4 bits per channel, unsigned normalized.

        // 8+8+8
        RGB8_UNorm, ///< 24-bit three-channel unsigned normalized.
        RGB8_SNorm, ///< 24-bit three-channel signed normalized.
        RGB8_UInt,  ///< 24-bit three-channel unsigned integer.
        RGB8_SInt,  ///< 24-bit three-channel signed integer.

        // 8+8+8+8
        RGBA8_UNorm, ///< 32-bit four-channel unsigned normalized.
        RGBA8_SNorm, ///< 32-bit four-channel signed normalized.
        RGBA8_UInt,  ///< 32-bit four-channel unsigned integer.
        RGBA8_SInt,  ///< 32-bit four-channel signed integer.
        RGBA8_sRGB,  ///< 32-bit four-channel sRGB-encoded, unsigned normalized.
        BGRA8_UNorm, ///< 32-bit four-channel unsigned normalized, BGRA memory order.
        BGRA8_sRGB,  ///< 32-bit four-channel sRGB-encoded, BGRA memory order.

        // 10+10+10+2
        R10G10B10A2_UNorm, ///< 32-bit packed RGB10/A2, unsigned normalized.
        R10G10B10A2_UInt,  ///< 32-bit packed RGB10/A2, unsigned integer.

        // 11+11+10
        R11G11B10_Float, ///< 32-bit packed three-channel float (11/11/10 bits).

        // 16 single
        R16_UNorm, ///< 16-bit single-channel unsigned normalized.
        R16_SNorm, ///< 16-bit single-channel signed normalized.
        R16_UInt,  ///< 16-bit single-channel unsigned integer.
        R16_SInt,  ///< 16-bit single-channel signed integer.
        R16_Float, ///< 16-bit single-channel floating point (half).

        // 16+16
        RG16_UNorm, ///< 16-bit two-channel unsigned normalized.
        RG16_SNorm, ///< 16-bit two-channel signed normalized.
        RG16_UInt,  ///< 16-bit two-channel unsigned integer.
        RG16_SInt,  ///< 16-bit two-channel signed integer.
        RG16_Float, ///< 16-bit two-channel floating point (half).

        // 16+16+16+16
        RGBA16_UNorm, ///< 16-bit four-channel unsigned normalized.
        RGBA16_SNorm, ///< 16-bit four-channel signed normalized.
        RGBA16_UInt,  ///< 16-bit four-channel unsigned integer.
        RGBA16_SInt,  ///< 16-bit four-channel signed integer.
        RGBA16_Float, ///< 16-bit four-channel floating point (half).

        // 32 single
        R32_UInt,  ///< 32-bit single-channel unsigned integer.
        R32_SInt,  ///< 32-bit single-channel signed integer.
        R32_Float, ///< 32-bit single-channel floating point.

        // 32+32
        RG32_UInt,  ///< 32-bit two-channel unsigned integer.
        RG32_SInt,  ///< 32-bit two-channel signed integer.
        RG32_Float, ///< 32-bit two-channel floating point.

        // 32+32+32
        RGB32_UInt,  ///< 32-bit three-channel unsigned integer.
        RGB32_SInt,  ///< 32-bit three-channel signed integer.
        RGB32_Float, ///< 32-bit three-channel floating point.

        // 32+32+32+32
        RGBA32_UInt,  ///< 32-bit four-channel unsigned integer.
        RGBA32_SInt,  ///< 32-bit four-channel signed integer.
        RGBA32_Float, ///< 32-bit four-channel floating point.

        // BC compressed (4x4 blocks)
        BC1_UNorm, ///< BC1: RGB with optional 1-bit alpha, 4 bits per texel.
        BC1_sRGB,  ///< BC1 with sRGB transfer.
        BC2_UNorm, ///< BC2: RGB plus 4-bit alpha, 8 bits per texel.
        BC2_sRGB,  ///< BC2 with sRGB transfer.
        BC3_UNorm, ///< BC3: RGB plus interpolated alpha, 8 bits per texel.
        BC3_sRGB,  ///< BC3 with sRGB transfer.
        BC4_UNorm, ///< BC4: single-channel unsigned normalized, 4 bits per texel.
        BC4_SNorm, ///< BC4: single-channel signed normalized, 4 bits per texel.
        BC5_UNorm, ///< BC5: two-channel unsigned normalized, 8 bits per texel.
        BC5_SNorm, ///< BC5: two-channel signed normalized, 8 bits per texel.
        BC6H_UF16, ///< BC6H: HDR RGB unsigned half-float, 8 bits per texel.
        BC6H_SF16, ///< BC6H: HDR RGB signed half-float, 8 bits per texel.
        BC7_UNorm, ///< BC7: high-quality RGBA, 8 bits per texel.
        BC7_sRGB,  ///< BC7 with sRGB transfer.

        // Depth/stencil
        D16_UNorm,         ///< 16-bit unsigned normalized depth.
        D24_UNorm_S8_UInt, ///< 24-bit depth plus 8-bit stencil.
        D32_Float,         ///< 32-bit floating-point depth.
        D32_Float_S8_UInt, ///< 32-bit float depth plus 8-bit stencil.
    };

    /// @brief Format classification and size helpers.
    namespace Core {}

    /// @brief Static properties and block layout of pixel formats.
    namespace Core::FormatTraits
    {
        /// @brief Static properties of one Format value.
        struct API FormatInfo
        {
            const char* name;      ///< Human-readable format name (valid for the process lifetime).
            uint32_t bytesPerPixel;///< Bytes per texel for uncompressed formats; 0 for BC formats.
            uint32_t channels;     ///< Number of color/depth channels encoded by the format.
            bool hasDepth;         ///< True for depth or depth/stencil formats.
            bool hasStencil;       ///< True when a stencil channel is present.
            bool isSRGB;           ///< True for sRGB-encoded color formats.
            bool isInteger;        ///< True for unnormalized integer formats.
            bool isFloat;          ///< True for floating-point formats.
        };

        /// @brief Looks up the static properties of a format.
        /// @param f Format to query.
        /// @return Format information; an "<invalid>" entry for out-of-range values.
        [[nodiscard]] constexpr FormatInfo API GetFormatInfo(Format f) noexcept
        {
            switch (f)
            {
            case Format::Unknown: return {
                    .name = "Unknown",
                    .bytesPerPixel = 0,
                    .channels = 0,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::R8_UNorm: return {
                    .name = "R8_UNorm",
                    .bytesPerPixel = 1,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::R8_SNorm: return {
                    .name = "R8_SNorm",
                    .bytesPerPixel = 1,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::R8_UInt: return {
                    .name = "R8_UInt",
                    .bytesPerPixel = 1,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::R8_SInt: return {
                    .name = "R8_SInt",
                    .bytesPerPixel = 1,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RG8_UNorm: return {
                    .name = "RG8_UNorm",
                    .bytesPerPixel = 2,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RG8_SNorm: return {
                    .name = "RG8_SNorm",
                    .bytesPerPixel = 2,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RG8_UInt: return {
                    .name = "RG8_UInt",
                    .bytesPerPixel = 2,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RG8_SInt: return {
                    .name = "RG8_SInt",
                    .bytesPerPixel = 2,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGB8_SNorm: return {
                    .name = "RGB8_SNorm",
                    .bytesPerPixel = 3,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGB8_UInt: return {
                    .name = "RGB8_UInt",
                    .bytesPerPixel = 3,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGB8_SInt: return {
                    .name = "RGB8_SInt",
                    .bytesPerPixel = 3,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::BGRA4_UNorm: return {
                    .name = "BGRA4_UNorm",
                    .bytesPerPixel = 2,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RGB8_UNorm: return {
                    .name = "RGB8_UNorm",
                    .bytesPerPixel = 3,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RGBA8_UNorm: return {
                    .name = "RGBA8_UNorm",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RGBA8_SNorm: return {
                    .name = "RGBA8_SNorm",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RGBA8_UInt: return {
                    .name = "RGBA8_UInt",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGBA8_SInt: return {
                    .name = "RGBA8_SInt",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGBA8_sRGB: return {
                    .name = "RGBA8_sRGB",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = true,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BGRA8_UNorm: return {
                    .name = "BGRA8_UNorm",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BGRA8_sRGB: return {
                    .name = "BGRA8_sRGB",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = true,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::R10G10B10A2_UNorm: return {
                    .name = "R10G10B10A2_UNorm",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::R10G10B10A2_UInt: return {
                    .name = "R10G10B10A2_UInt",
                    .bytesPerPixel = 4,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::R11G11B10_Float: return {
                    .name = "R11G11B10_Float",
                    .bytesPerPixel = 4,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::R16_UNorm: return {
                    .name = "R16_UNorm",
                    .bytesPerPixel = 2,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::R16_SNorm: return {
                    .name = "R16_SNorm",
                    .bytesPerPixel = 2,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::R16_UInt: return {
                    .name = "R16_UInt",
                    .bytesPerPixel = 2,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::R16_SInt: return {
                    .name = "R16_SInt",
                    .bytesPerPixel = 2,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::R16_Float: return {
                    .name = "R16_Float",
                    .bytesPerPixel = 2,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::RG16_UNorm: return {
                    .name = "RG16_UNorm",
                    .bytesPerPixel = 4,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RG16_SNorm: return {
                    .name = "RG16_SNorm",
                    .bytesPerPixel = 4,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RG16_UInt: return {
                    .name = "RG16_UInt",
                    .bytesPerPixel = 4,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RG16_SInt: return {
                    .name = "RG16_SInt",
                    .bytesPerPixel = 4,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RG16_Float: return {
                    .name = "RG16_Float",
                    .bytesPerPixel = 4,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::RGBA16_UNorm: return {
                    .name = "RGBA16_UNorm",
                    .bytesPerPixel = 8,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RGBA16_SNorm: return {
                    .name = "RGBA16_SNorm",
                    .bytesPerPixel = 8,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::RGBA16_UInt: return {
                    .name = "RGBA16_UInt",
                    .bytesPerPixel = 8,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGBA16_SInt: return {
                    .name = "RGBA16_SInt",
                    .bytesPerPixel = 8,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGBA16_Float: return {
                    .name = "RGBA16_Float",
                    .bytesPerPixel = 8,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::R32_UInt: return {
                    .name = "R32_UInt",
                    .bytesPerPixel = 4,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::R32_SInt: return {
                    .name = "R32_SInt",
                    .bytesPerPixel = 4,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::R32_Float: return {
                    .name = "R32_Float",
                    .bytesPerPixel = 4,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::RG32_UInt: return {
                    .name = "RG32_UInt",
                    .bytesPerPixel = 8,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RG32_SInt: return {
                    .name = "RG32_SInt",
                    .bytesPerPixel = 8,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RG32_Float: return {
                    .name = "RG32_Float",
                    .bytesPerPixel = 8,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::RGB32_UInt: return {
                    .name = "RGB32_UInt",
                    .bytesPerPixel = 12,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGB32_SInt: return {
                    .name = "RGB32_SInt",
                    .bytesPerPixel = 12,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGB32_Float: return {
                    .name = "RGB32_Float",
                    .bytesPerPixel = 12,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::RGBA32_UInt: return {
                    .name = "RGBA32_UInt",
                    .bytesPerPixel = 16,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGBA32_SInt: return {
                    .name = "RGBA32_SInt",
                    .bytesPerPixel = 16,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = true,
                    .isFloat = false
                };
            case Format::RGBA32_Float: return {
                    .name = "RGBA32_Float",
                    .bytesPerPixel = 16,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::BC1_UNorm: return {
                    .name = "BC1_UNorm",
                    .bytesPerPixel = 0,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC1_sRGB: return {
                    .name = "BC1_sRGB",
                    .bytesPerPixel = 0,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = true,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC2_UNorm: return {
                    .name = "BC2_UNorm",
                    .bytesPerPixel = 0,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC2_sRGB: return {
                    .name = "BC2_sRGB",
                    .bytesPerPixel = 0,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = true,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC3_UNorm: return {
                    .name = "BC3_UNorm",
                    .bytesPerPixel = 0,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC3_sRGB: return {
                    .name = "BC3_sRGB",
                    .bytesPerPixel = 0,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = true,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC4_UNorm: return {
                    .name = "BC4_UNorm",
                    .bytesPerPixel = 0,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC4_SNorm: return {
                    .name = "BC4_SNorm",
                    .bytesPerPixel = 0,
                    .channels = 1,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC5_UNorm: return {
                    .name = "BC5_UNorm",
                    .bytesPerPixel = 0,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC5_SNorm: return {
                    .name = "BC5_SNorm",
                    .bytesPerPixel = 0,
                    .channels = 2,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC6H_UF16: return {
                    .name = "BC6H_UF16",
                    .bytesPerPixel = 0,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::BC6H_SF16: return {
                    .name = "BC6H_SF16",
                    .bytesPerPixel = 0,
                    .channels = 3,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::BC7_UNorm: return {
                    .name = "BC7_UNorm",
                    .bytesPerPixel = 0,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::BC7_sRGB: return {
                    .name = "BC7_sRGB",
                    .bytesPerPixel = 0,
                    .channels = 4,
                    .hasDepth = false,
                    .hasStencil = false,
                    .isSRGB = true,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::D16_UNorm: return {
                    .name = "D16_UNorm",
                    .bytesPerPixel = 2,
                    .channels = 1,
                    .hasDepth = true,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::D24_UNorm_S8_UInt: return {
                    .name = "D24_UNorm_S8_UInt",
                    .bytesPerPixel = 4,
                    .channels = 1,
                    .hasDepth = true,
                    .hasStencil = true,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = false
                };
            case Format::D32_Float: return {
                    .name = "D32_Float",
                    .bytesPerPixel = 4,
                    .channels = 1,
                    .hasDepth = true,
                    .hasStencil = false,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            case Format::D32_Float_S8_UInt: return {
                    .name = "D32_Float_S8_UInt",
                    .bytesPerPixel = 8,
                    .channels = 1,
                    .hasDepth = true,
                    .hasStencil = true,
                    .isSRGB = false,
                    .isInteger = false,
                    .isFloat = true
                };
            }
            return {
                .name = "<invalid>",
                .bytesPerPixel = 0,
                .channels = 0,
                .hasDepth = false,
                .hasStencil = false,
                .isSRGB = false,
                .isInteger = false,
                .isFloat = false
            };
        }

        /// @brief Returns the byte size of one texel for uncompressed formats.
        /// @param f Format to query.
        /// @return Bytes per texel, or 0 for BC-compressed and unknown formats.
        [[nodiscard]] constexpr uint32_t BytesPerPixel(Format f) noexcept
        {
            return GetFormatInfo(f).bytesPerPixel;
        }

        /// @brief True when the format contains a depth channel.
        /// @param f Format to test.
        /// @return True when the format has a depth channel.
        [[nodiscard]] constexpr bool IsDepthFormat(Format f) noexcept
        {
            return GetFormatInfo(f).hasDepth;
        }

        /// @brief Dimensions and byte size of one format block.
        struct API BlockInfo
        {
            uint32_t width; ///< Block width in texels (4 for BC formats, otherwise 1).
            uint32_t height;///< Block height in texels (4 for BC formats, otherwise 1).
            uint32_t bytes; ///< Bytes occupied by one block.
        };

        /// @brief Returns the block layout of a format.
        /// @param f Format to query.
        /// @return Block dimensions and byte size; 1x1 blocks for uncompressed formats.
        /// @note Compressed formats are addressed in 4x4 blocks; uncompressed ones
        /// use 1x1 blocks whose size matches BytesPerPixel.
        [[nodiscard]] constexpr BlockInfo GetBlockInfo(Format f) noexcept
        {
            switch (f)
            {
            case Format::BC1_UNorm:
            case Format::BC1_sRGB:
            case Format::BC4_UNorm:
            case Format::BC4_SNorm:
                return {4, 4, 8};

            case Format::BC2_UNorm:
            case Format::BC2_sRGB:
            case Format::BC3_UNorm:
            case Format::BC3_sRGB:
            case Format::BC5_UNorm:
            case Format::BC5_SNorm:
            case Format::BC6H_UF16:
            case Format::BC6H_SF16:
            case Format::BC7_UNorm:
            case Format::BC7_sRGB:
                return {4, 4, 16};

            default:
                return {1, 1, BytesPerPixel(f)};
            }
        }

        /// @brief True for BC block-compressed formats.
        /// @param f Format to test.
        /// @return True when the format is block-compressed.
        [[nodiscard]] constexpr bool IsCompressedFormat(Format f) noexcept
        {
            return GetBlockInfo(f).width > 1;
        }

        /// @brief Bytes per row of a mip level for the given width.
        /// @param f Format of the mip level.
        /// @param width Width in texels of the mip level.
        /// @return Row size in bytes, including padding to whole compressed blocks.
        [[nodiscard]] constexpr uint64_t RowPitch(Format f, uint32_t width) noexcept
        {
            const BlockInfo block = GetBlockInfo(f);
            return static_cast<uint64_t>((width + block.width - 1) / block.width) * block.bytes;
        }

        /// @brief Total byte size of one 2D slice, including whole compressed blocks.
        /// @param f Format of the slice.
        /// @param width Width in texels of the mip level.
        /// @param height Height in texels of the mip level.
        /// @return Slice size in bytes.
        [[nodiscard]] constexpr uint64_t SlicePitch(Format f, uint32_t width, uint32_t height) noexcept
        {
            const BlockInfo block = GetBlockInfo(f);
            const uint64_t rows = (height + block.height - 1) / block.height;
            return RowPitch(f, width) * rows;
        }
    }
}
