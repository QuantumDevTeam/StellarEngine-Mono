// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>
#include <type_traits>

/// @file Graphic/Contracts/Core/Enums/Blend.h
/// @brief Blend factors, blend operations, logic operations and color write masks.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Multiplier applied to a source or destination color during blending.
    ///
    /// The final pixel is computed as `src * srcFactor op dst * dstFactor`.
    /// Backends map these values to their native blend-factor enumerations;
    /// unsupported combinations are rejected when the pipeline is created.
    enum class API BlendFactor : uint32_t
    {
        Zero,              ///< (0, 0, 0, 0) — the term is ignored.
        One,               ///< (1, 1, 1, 1) — the term is used unchanged.
        SrcColor,          ///< Source color (RGB and A from the shader output).
        InvSrcColor,       ///< One minus the source color.
        SrcAlpha,          ///< Source alpha replicated to all channels.
        InvSrcAlpha,       ///< One minus the source alpha.
        DestAlpha,         ///< Destination alpha replicated to all channels.
        InvDestAlpha,      ///< One minus the destination alpha.
        DestColor,         ///< Destination color (RGB and A from the render target).
        InvDestColor,      ///< One minus the destination color.
        SrcAlphaSaturate,  ///< min(source alpha, 1 - destination alpha).
        SimpleBlendFactor, ///< Constant blend factor supplied by the command list.
        InvBlendFactor,    ///< One minus the constant blend factor.
        Src1Color,         ///< Second source color (dual-source blending, RGB).
        InvSrc1Color,      ///< One minus the second source color.
        Src1Alpha,         ///< Second source alpha (dual-source blending).
        InvSrc1Alpha,      ///< One minus the second source alpha.
        AlphaFactor,       ///< Constant alpha factor supplied by the command list.
        InvAlphaFactor,    ///< One minus the constant alpha factor.
    };

    /// @brief Arithmetic operation combining the source and destination terms.
    enum class API BlendOp : uint32_t
    {
        Add,         ///< source * srcFactor + destination * dstFactor.
        Subtract,    ///< source * srcFactor - destination * dstFactor.
        RevSubtract, ///< destination * dstFactor - source * srcFactor.
        Min,         ///< Component-wise minimum of both terms.
        Max,         ///< Component-wise maximum of both terms.
    };

    /// @brief Bitwise operation used instead of arithmetic blending.
    ///
    /// Logic operations are available only on backends/formats that support
    /// them (typically non-floating-point render targets). They are mutually
    /// exclusive with arithmetic blending on a target.
    enum class API LogicOp : uint32_t
    {
        Clear,        ///< 0.
        Set,          ///< 1.
        Copy,         ///< source.
        CopyInverted, ///< NOT source.
        Noop,         ///< destination (no change).
        Invert,       ///< NOT destination.
        And,          ///< source AND destination.
        Nand,         ///< NOT (source AND destination).
        Or,           ///< source OR destination.
        Nor,          ///< NOT (source OR destination).
        Xor,          ///< source XOR destination.
        Equiv,        ///< NOT (source XOR destination).
        AndReverse,   ///< source AND NOT destination.
        AndInverted,  ///< NOT source AND destination.
        OrReverse,    ///< source OR NOT destination.
        OrInverted,   ///< NOT source OR destination.
    };

    /// @brief Per-channel write mask for a render target.
    ///
    /// Combining flags enables writes to the corresponding channels; a channel
    /// whose bit is clear keeps its previous value. The mask is applied after
    /// blending and is useful for depth-only or single-channel passes.
    enum class API ColorWriteMask : uint32_t
    {
        None  = 0,                            ///< No channel is written.
        Red   = 1u << 0,                      ///< Write the red channel.
        Green = 1u << 1,                      ///< Write the green channel.
        Blue  = 1u << 2,                      ///< Write the blue channel.
        Alpha = 1u << 3,                      ///< Write the alpha channel.
        All   = Red | Green | Blue | Alpha,   ///< Write all four channels.
    };
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    /// @brief Enables bitwise flag operators for Graphics::ColorWriteMask.
    template <> struct IsFlagEnum<ColorWriteMask> : std::true_type {};
}
