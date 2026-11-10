// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Color4.h
/// @brief Four-component linear color value used by clear, blend and style APIs.
#pragma once

#include <array>
#include <tuple>

#include "Math/Vector/Vector4.h" // NOLINT

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Linear RGBA color with floating-point components.
    ///
    /// Components are normally in [0, 1]; values outside that range are kept
    /// as-is so HDR colors can be represented. The type is trivially copyable,
    /// constexpr-friendly and ordered lexicographically by (R, G, B, A).
    struct API Color4 final
    {
        /// @brief Red component.
        float R = 0.0f;

        /// @brief Green component.
        float G = 0.0f;

        /// @brief Blue component.
        float B = 0.0f;

        /// @brief Alpha component.
        float A = 1.0f;

        // construction

        /// @brief Creates opaque black (0, 0, 0, 1).
        constexpr explicit Color4() noexcept = default;

        /// @brief Creates a color from a four-element array.
        /// @param arr Array in R, G, B, A order; must contain at least four values.
        constexpr explicit Color4(float arr[4]) noexcept
            : R{arr[0]},
              G{arr[1]},
              B{arr[2]},
              A{arr[3]} {}

        /// @brief Broadcasts one value to every component.
        /// @param v Value used for R, G, B and A.
        constexpr explicit Color4(float v) noexcept
            : R{v},
              G{v},
              B{v},
              A{v} {}

        /// @brief Broadcasts one value to every component.
        /// @param v Value used for R, G and B.
        /// @param a Value used for A.
        constexpr explicit Color4(float v, float a) noexcept
            : R{v},
              G{v},
              B{v},
              A{a} {}

        /// @brief Creates a color from individual components.
        /// @param r Red component.
        /// @param g Green component.
        /// @param b Blue component.
        /// @param a Alpha component.
        constexpr Color4(float r, float g, float b, float a) noexcept
            : R{r},
              G{g},
              B{b},
              A{a} {}

        /// @brief Creates a color from individual components.
        /// @param v color Vector supplying R, G, B and A.
        constexpr Color4(Link<Math::Vectors::Vector4<float>> v) noexcept
            : R{v.X},
              G{v.Y},
              B{v.Z},
              A{v.W} {}

        /// @brief Copies all four components.
        constexpr Color4(const Color4&) noexcept = default;
        /// @brief Moves all four components; the source is left unchanged.
        constexpr Color4(Color4&&) noexcept = default;
        /// @brief Copies all four components.
        /// @return Reference to this color.
        constexpr Color4& operator=(const Color4&) noexcept = default;
        /// @brief Moves all four components.
        /// @return Reference to this color.
        constexpr Color4& operator=(Color4&&) noexcept = default;

        /// @brief Trivial destructor.
        constexpr ~Color4() noexcept = default;

        // spaceship

        /// @brief Lexicographic comparison of the four components.
        /// @return Ordering result suitable for associative containers.
        constexpr auto operator<=>(const Color4&) const noexcept = default;

        // values

        /// @brief Returns the components as an (R, G, B, A) tuple.
        /// @return Tuple containing (R, G, B, A).
        constexpr std::tuple<float, float, float, float> unpack() const
        {
            return std::make_tuple(R, G, B, A);
        }

        /// @brief Returns all fields as a braced list.
        /// @return Array containing R, G, B and A.
        constexpr std::array<float, 4> arr() const noexcept { return {R, G, B, A}; }

        /// @brief Adds a scalar to every component.
        /// @param o Value added to each component.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 Add(float o) const noexcept { return Color4{R + o, G + o, B + o, A + o}; }

        /// @brief Subtracts a scalar from every component.
        /// @param o Value subtracted from each component.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 Sub(float o) const noexcept { return Color4{R - o, G - o, B - o, A - o}; }

        /// @brief Multiplies every component by a scalar.
        /// @param o Scale factor applied to each component.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 Mul(float o) const noexcept { return Color4{R * o, G * o, B * o, A * o}; }

        /// @brief Divides every component by a scalar.
        /// @param o Divisor; must not be zero.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 Div(float o) const noexcept { return Color4{R / o, G / o, B / o, A / o}; }

        /// @brief Component-wise addition of two colors.
        /// @param o Color to add.
        /// @return Component-wise sum.
        [[nodiscard]] constexpr Color4 Add(Link<const Color4> o) const noexcept
        {
            return Color4{R + o.R, G + o.G, B + o.B, A + o.A};
        }

        /// @brief Component-wise subtraction of two colors.
        /// @param o Color to subtract.
        /// @return Component-wise difference.
        [[nodiscard]] constexpr Color4 Sub(Link<const Color4> o) const noexcept
        {
            return Color4{R - o.R, G - o.G, B - o.B, A - o.A};
        }

        /// @brief Component-wise multiplication of two colors.
        /// @param o Color to multiply by.
        /// @return Component-wise product.
        [[nodiscard]] constexpr Color4 Mul(Link<const Color4> o) const noexcept
        {
            return Color4{R * o.R, G * o.G, B * o.B, A * o.A};
        }

        /// @brief Component-wise division of two colors.
        /// @param o Color to divide by.
        /// @return Component-wise quotient.
        /// @note The caller is responsible for avoiding division by zero.
        [[nodiscard]] constexpr Color4 Div(Link<const Color4> o) const noexcept
        {
            return Color4{R / o.R, G / o.G, B / o.B, A / o.A};
        }

        /// @brief Adds a scalar to every component.
        /// @param o Value added to each component.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 operator+(float o) const noexcept { return Add(o); }
        /// @brief Subtracts a scalar from every component.
        /// @param o Value subtracted from each component.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 operator-(float o) const noexcept { return Sub(o); }
        /// @brief Multiplies every component by a scalar.
        /// @param o Scale factor applied to each component.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 operator*(float o) const noexcept { return Mul(o); }
        /// @brief Divides every component by a scalar.
        /// @param o Divisor; must not be zero.
        /// @return New color with the scalar applied.
        [[nodiscard]] constexpr Color4 operator/(float o) const noexcept { return Div(o); }

        /// @brief Component-wise addition.
        /// @param o Color to add.
        /// @return Component-wise sum.
        [[nodiscard]] constexpr Color4 operator+(Link<const Color4> o) const noexcept { return Add(o); }
        /// @brief Component-wise subtraction.
        /// @param o Color to subtract.
        /// @return Component-wise difference.
        [[nodiscard]] constexpr Color4 operator-(Link<const Color4> o) const noexcept { return Sub(o); }
        /// @brief Component-wise multiplication.
        /// @param o Color to multiply by.
        /// @return Component-wise product.
        [[nodiscard]] constexpr Color4 operator*(Link<const Color4> o) const noexcept { return Mul(o); }
        /// @brief Component-wise division.
        /// @param o Color to divide by.
        /// @return Component-wise quotient.
        [[nodiscard]] constexpr Color4 operator/(Link<const Color4> o) const noexcept { return Div(o); }

        // unary minus
        /// @brief Returns the photographic negative (1 - component) of the color.
        /// @return Color whose components are `1 - component`.
        [[nodiscard]] constexpr Color4 operator-() const noexcept
        {
            return Color4{1.0f - R, 1.0f - G, 1.0f - B, 1.0f - A};
        }
    };
}
