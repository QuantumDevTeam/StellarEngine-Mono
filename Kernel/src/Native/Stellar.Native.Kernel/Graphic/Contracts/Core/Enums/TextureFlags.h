// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>
#include <type_traits>

/// @file Graphic/Contracts/Core/Enums/TextureFlags.h
/// @brief Texture dimensionality and capability flags.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Dimensionality and view kind of a texture.
    enum class API TextureType : uint32_t
    {
        Texture1D = 0, ///< One-dimensional texture (width only).
        Texture2D,     ///< Two-dimensional texture (width and height).
        Texture3D,     ///< Three-dimensional texture (width, height and depth).
        TextureCube,   ///< Cube map; array slices are grouped in six faces.
    };

    /// @brief Declares how a texture may be bound and which views it needs.
    ///
    /// The flags are a creation-time contract. The backend validates that the
    /// selected format supports the requested capabilities (for example,
    /// depth formats cannot be unordered-access targets on most APIs).
    enum class TextureFlags : uint32_t
    {
        None                    = 0,          ///< No special capability.
        AllowRenderTarget       = 1u << 0,    ///< Can be bound as a color render target (RTV).
        AllowDepthStencil       = 1u << 1,    ///< Can be bound as a depth/stencil target (DSV).
        AllowUnorderedAccess    = 1u << 2,    ///< Can be bound as an unordered access view (UAV).
        AllowShaderResource     = 1u << 3,    ///< Can be read through a shader resource view (SRV).
        AllowSimultaneousAccess = 1u << 4,    ///< Can be read and written concurrently (backend-dependent).
        DenyShaderResource      = 1u << 5,    ///< Explicitly forbid SRV creation.
        GenerateMips            = 1u << 6,    ///< Allocate mip levels for runtime mip generation.
    };
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    /// @brief Enables bitwise flag operators for Graphics::TextureFlags.
    template <>
    struct IsFlagEnum<TextureFlags> : std::true_type {};
}
