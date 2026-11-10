// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Viewport.h
/// @brief Viewport rectangle used by rendering contexts and command lists.
#pragma once

#include <tuple>

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Rectangle in render-target space that maps NDC to pixels.
    ///
    /// X/Y define the top-left corner in pixels, Width/Height the extents.
    /// MinDepth/MaxDepth select the depth range written by the rasterizer;
    /// the usual values are 0 and 1. The type is trivially copyable and
    /// ordered lexicographically by its six components.
    struct API Viewport final
    {
        /// @brief Left edge in render-target pixels.
        float X = 0.0f;

        /// @brief Top edge in render-target pixels.
        float Y = 0.0f;

        /// @brief Viewport width in pixels.
        float Width = 0.0f;

        /// @brief Viewport height in pixels.
        float Height = 0.0f;

        /// @brief Depth value mapped to NDC z = 0.
        float MinDepth = 0.0f;

        /// @brief Depth value mapped to NDC z = 1.
        float MaxDepth = 1.0f;

        // construction
        /// @brief Creates an empty viewport at the origin with depth range 0..1.
        constexpr Viewport() noexcept = default;

        // from constants
        /// @brief Creates a viewport with the default depth range 0..1.
        /// @param x Left edge in pixels.
        /// @param y Top edge in pixels.
        /// @param width Width in pixels.
        /// @param height Height in pixels.
        constexpr Viewport(float x, float y, float width, float height) noexcept
            : X(x),
              Y(y),
              Width(width),
              Height(height) {}

        /// @brief Creates a viewport with an explicit depth range.
        /// @param x Left edge in pixels.
        /// @param y Top edge in pixels.
        /// @param width Width in pixels.
        /// @param height Height in pixels.
        /// @param minDepth Depth value mapped to NDC z = 0.
        /// @param maxDepth Depth value mapped to NDC z = 1.
        constexpr Viewport(float x, float y, float width, float height, float minDepth, float maxDepth) noexcept
            : X(x),
              Y(y),
              Width(width),
              Height(height),
              MinDepth(minDepth),
              MaxDepth(maxDepth) {}

        /// @brief Copies all six components.
        constexpr Viewport(const Viewport&) noexcept = default;
        /// @brief Moves all six components; the source is left unchanged.
        constexpr Viewport(Viewport&&) noexcept = default;
        /// @brief Copies all six components.
        /// @return Reference to this viewport.
        constexpr Viewport& operator=(const Viewport&) noexcept = default;
        /// @brief Moves all six components.
        /// @return Reference to this viewport.
        constexpr Viewport& operator=(Viewport&&) noexcept = default;

        /// @brief Trivial destructor.
        constexpr ~Viewport() noexcept = default;

        // spaceship

        /// @brief Lexicographic comparison of all six components.
        /// @return Ordering result suitable for associative containers.
        constexpr auto operator<=>(const Viewport&) const noexcept = default;

        // values

        /// @brief Returns (X, Y, Width, Height, MinDepth, MaxDepth) as a tuple.
        /// @return Tuple with all six viewport components.
        constexpr std::tuple<float, float, float, float, float, float> unpack() const
        {
            return std::make_tuple(X, Y, Width, Height, MinDepth, MaxDepth);
        }

        // scissor edges

        /// @brief Left edge (X).
        /// @return X coordinate in pixels.
        [[nodiscard]] constexpr float getLeft() const noexcept { return X; }
        /// @brief Right edge (X + Width).
        /// @return Right edge in pixels.
        [[nodiscard]] constexpr float getRight() const noexcept { return X + Width; }
        /// @brief Top edge (Y + Height).
        /// @return Bottom edge in pixels under Y-down coordinates.
        [[nodiscard]] constexpr float getTop() const noexcept { return Y + Height; }
        /// @brief Bottom edge (Y).
        /// @return Top edge in pixels under Y-down coordinates.
        [[nodiscard]] constexpr float getBottom() const noexcept { return Y; }
    };
}
