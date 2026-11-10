// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Math/Vector/Vector4.h
/// @brief Four-component arithmetic vector.
///
/// Vector4 stores X/Y/Z/W components and provides component-wise arithmetic
/// plus 2D/3D projections. Arithmetic operations are constexpr and noexcept.
/// The type is generic: W has no intrinsic meaning and depends on the consumer
/// (for example homogeneous coordinates or RGBA color).
///
/// @note This header relies on the kernel prelude for the Scalar concept and
///       the API macro. Include Kernel/kernel.h before this header when it is
///       used from a standalone translation unit.
#pragma once

#include <cmath>
#include <tuple>

#include "Vector2.h"
#include "Vector3.h"

namespace Stellar::Native::Math::Vectors
{
    /// @brief Four-dimensional vector with arithmetic components.
    /// @tparam T Component type; must satisfy Concepts::Scalar
    ///         (std::is_arithmetic_v<T>).
    ///
    /// Components are stored in X/Y/Z/W order. Comparison is lexicographic
    /// (X, then Y, Z, W), which gives a strict total order but is unrelated to
    /// geometric length. The type has no virtual functions and owns no
    /// resources.
    template <Scalar T>
    struct API Vector4 final
    {
        T X{}; ///< First component; defaults to zero.
        T Y{}; ///< Second component; defaults to zero.
        T Z{}; ///< Third component; defaults to zero.
        T W{}; ///< Fourth component; defaults to zero.

        // construction

        /// @brief Creates a zero vector (all components zero).
        constexpr explicit Vector4() noexcept = default;

        /// @brief Initializes the vector from a four-element C array.
        /// @param v Array whose first four elements become X, Y, Z and W.
        /// @pre @p v points to at least four elements of type @p T.
        /// @note The array is read once and is not retained.
        constexpr explicit Vector4(T v[4]) noexcept
            : X(v[0]),
              Y(v[1]),
              Z(v[2]),
              W(v[3]) {}

        /// @brief Broadcasts one scalar to all four components.
        /// @param v Value copied into X, Y, Z and W.
        constexpr explicit Vector4(T v) noexcept
            : X{v},
              Y{v},
              Z{v},
              W{v} {}

        /// @brief Creates a vector from explicit components.
        /// @param x Value for X.
        /// @param y Value for Y.
        /// @param z Value for Z.
        /// @param w Value for W.
        constexpr Vector4(T x, T y, T z, T w) noexcept
            : X{x},
              Y{y},
              Z{z},
              W{w} {}

        /// @brief Creates a vector from two 2D vectors.
        /// @param xy Vector supplying X and Y.
        /// @param zw Vector supplying Z and W.
        constexpr Vector4(Link<const Vector2<T>> xy, Link<const Vector2<T>> zw) noexcept
            : X{xy.X},
              Y{xy.Y},
              Z{zw.X},
              W{zw.Y} {}

        /// @brief Creates a vector from 3D vector and W component.
        /// @param xyz Vector supplying X, Y and Z.
        /// @param w Value for W.
        constexpr Vector4(Link<const Vector3<T>> xyz, T w) noexcept
            : X{xyz.X},
              Y{xyz.Y},
              Z{xyz.Z},
              W{w} {}

        // copy/move

        /// @brief Copies all components from another vector.
        constexpr Vector4(const Vector4&) noexcept = default;
        /// @brief Moves all components from another vector.
        /// @note Scalars are copied, so move and copy have identical behavior.
        constexpr Vector4(Vector4&&) noexcept = default;
        /// @brief Copies all components from another vector.
        /// @return Reference to this vector.
        constexpr Vector4& operator=(const Vector4&) noexcept = default;
        /// @brief Moves all components from another vector.
        /// @return Reference to this vector.
        constexpr Vector4& operator=(Vector4&&) noexcept = default;

        /// @brief Trivial destructor; the vector owns no resources.
        constexpr ~Vector4() noexcept = default;

        // spaceship

        /// @brief Lexicographic three-way comparison of X, Y, Z, then W.
        /// @return Ordering result suitable for associative containers.
        /// @note This is not a length comparison.
        constexpr auto operator<=>(const Vector4&) const noexcept = default;

        // values

        /// @brief Returns all components as a tuple.
        /// @return Tuple containing (X, Y, Z, W).
        constexpr std::tuple<T, T, T, T> unpack() const { return std::make_tuple(X, Y, Z, W); }

        // math

        /// @brief Computes the dot product with another vector.
        /// @param o Other vector.
        /// @return X * o.X + Y * o.Y + Z * o.Z + W * o.W.
        [[nodiscard]] constexpr T dot(Link<const Vector4> o) const noexcept
        {
            return X * o.X + Y * o.Y + Z * o.Z + W * o.W;
        }

        /// @brief Returns the squared Euclidean length.
        /// @return X * X + Y * Y + Z * Z + W * W, computed without a square root.
        [[nodiscard]] constexpr T LengthSq() const noexcept { return dot(*this); }

        /// @brief Returns the Euclidean length.
        /// @return sqrt(LengthSq()). For integral T the square root is computed
        ///         in double and converted back to T, truncating the fraction.
        [[nodiscard]] constexpr T Length() const noexcept { return std::sqrt(LengthSq()); }

        /// @brief Adds a scalar to each component.
        /// @param o Value added to every component.
        /// @return New vector containing the sums.
        [[nodiscard]] constexpr Vector4 Add(T o) const noexcept { return Vector4{X + o, Y + o, Z + o, W + o}; }

        /// @brief Subtracts a scalar from each component.
        /// @param o Value subtracted from every component.
        /// @return New vector containing the differences.
        [[nodiscard]] constexpr Vector4 Sub(T o) const noexcept { return Vector4{X - o, Y - o, Z - o, W - o}; }

        /// @brief Multiplies each component by a scalar.
        /// @param o Factor applied to every component.
        /// @return New vector containing the products.
        [[nodiscard]] constexpr Vector4 Mul(T o) const noexcept { return Vector4{X * o, Y * o, Z * o, W * o}; }

        /// @brief Divides each component by a scalar.
        /// @param o Divisor applied to every component.
        /// @return New vector containing the quotients.
        /// @note No zero check is performed; division by zero follows T's rules.
        [[nodiscard]] constexpr Vector4 Div(T o) const noexcept { return Vector4{X / o, Y / o, Z / o, W / o}; }

        /// @brief Adds another vector component-wise.
        /// @param o Vector whose components are added.
        /// @return New vector containing the sums.
        [[nodiscard]] constexpr Vector4 Add(Link<const Vector4> o) const noexcept
        {
            return Vector4{X + o.X, Y + o.Y, Z + o.Z, W + o.W};
        }

        /// @brief Subtracts another vector component-wise.
        /// @param o Vector whose components are subtracted.
        /// @return New vector containing the differences.
        [[nodiscard]] constexpr Vector4 Sub(Link<const Vector4> o) const noexcept
        {
            return Vector4{X - o.X, Y - o.Y, Z - o.Z, W - o.W};
        }

        /// @brief Multiplies by another vector component-wise.
        /// @param o Vector whose components are used as factors.
        /// @return New vector containing the products.
        [[nodiscard]] constexpr Vector4 Mul(Link<const Vector4> o) const noexcept
        {
            return Vector4{X * o.X, Y * o.Y, Z * o.Z, W * o.W};
        }

        /// @brief Divides by another vector component-wise.
        /// @param o Vector whose components are used as divisors.
        /// @return New vector containing the quotients.
        /// @note No zero check is performed on any component.
        [[nodiscard]] constexpr Vector4 Div(Link<const Vector4> o) const noexcept
        {
            return Vector4{X / o.X, Y / o.Y, Z / o.Z, W / o.W};
        }

        /// @brief Adds a scalar to all components (same as Add(o)).
        /// @param o Value added to each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector4 operator+(T o) const noexcept { return Add(o); }
        /// @brief Subtracts a scalar from all components (same as Sub(o)).
        /// @param o Value subtracted from each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector4 operator-(T o) const noexcept { return Sub(o); }
        /// @brief Multiplies all components by a scalar (same as Mul(o)).
        /// @param o Scale factor applied to each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector4 operator*(T o) const noexcept { return Mul(o); }
        /// @brief Divides all components by a scalar (same as Div(o)).
        /// @param o Divisor; must not be zero.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector4 operator/(T o) const noexcept { return Div(o); }

        /// @brief Adds another vector component-wise (same as Add(o)).
        /// @param o Vector to add.
        /// @return Component-wise sum.
        [[nodiscard]] constexpr Vector4 operator+(Link<const Vector4> o) const noexcept { return Add(o); }
        /// @brief Subtracts another vector component-wise (same as Sub(o)).
        /// @param o Vector to subtract.
        /// @return Component-wise difference.
        [[nodiscard]] constexpr Vector4 operator-(Link<const Vector4> o) const noexcept { return Sub(o); }
        /// @brief Multiplies by another vector component-wise (same as Mul(o)).
        /// @param o Vector to multiply by.
        /// @return Component-wise product.
        [[nodiscard]] constexpr Vector4 operator*(Link<const Vector4> o) const noexcept { return Mul(o); }
        /// @brief Divides by another vector component-wise (same as Div(o)).
        /// @param o Vector whose components are used as divisors.
        /// @return Component-wise quotient.
        [[nodiscard]] constexpr Vector4 operator/(Link<const Vector4> o) const noexcept { return Div(o); }

        // shortcodes

        /// @brief Returns the XY projection.
        /// @return Two-component vector (X, Y).
        [[nodiscard]] constexpr Vector2<T> xy() const noexcept { return {X, Y}; }
        /// @brief Returns the XZ projection.
        /// @return Two-component vector (X, Z).
        [[nodiscard]] constexpr Vector2<T> xz() const noexcept { return {X, Z}; }
        /// @brief Returns the XW projection.
        /// @return Two-component vector (X, W).
        [[nodiscard]] constexpr Vector2<T> xw() const noexcept { return {X, W}; }
        /// @brief Returns the YZ projection.
        /// @return Two-component vector (Y, Z).
        [[nodiscard]] constexpr Vector2<T> yz() const noexcept { return {Y, Z}; }
        /// @brief Returns the YW projection.
        /// @return Two-component vector (Y, W).
        [[nodiscard]] constexpr Vector2<T> yw() const noexcept { return {Y, W}; }
        /// @brief Returns the ZW projection.
        /// @return Two-component vector (Z, W).
        [[nodiscard]] constexpr Vector2<T> zw() const noexcept { return {Z, W}; }

        /// @brief Returns the XYZ projection.
        /// @return Three-component vector (X, Y, Z).
        [[nodiscard]] constexpr Vector3<T> xyz() const noexcept { return {X, Y, Z}; }
        /// @brief Returns the XYW projection.
        /// @return Three-component vector (X, Y, W).
        [[nodiscard]] constexpr Vector3<T> xyw() const noexcept { return {X, Y, W}; }
        /// @brief Returns the XZW projection.
        /// @return Three-component vector (X, Z, W).
        [[nodiscard]] constexpr Vector3<T> xzw() const noexcept { return {X, Z, W}; }
        /// @brief Returns the YZW projection.
        /// @return Three-component vector (Y, Z, W).
        [[nodiscard]] constexpr Vector3<T> yzw() const noexcept { return {Y, Z, W}; }

        // unary minus

        /// @brief Returns the negated vector (-X, -Y, -Z, -W).
        /// @return Negated vector.
        [[nodiscard]] constexpr Vector4 operator-() const noexcept { return {-X, -Y, -Z, -W}; }
    };
}
