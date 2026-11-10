// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Math/Vector/Vector3.h
/// @brief Three-component arithmetic vector.
///
/// Vector3 stores X/Y/Z components and adds the cross product and 2D
/// projections to the common vector operations. Arithmetic is component-wise
/// unless stated otherwise; arithmetic operations are constexpr and noexcept.
///
/// @note This header relies on the kernel prelude for the Scalar concept and
///       the API macro. Include Kernel/kernel.h before this header when it is
///       used from a standalone translation unit.
#pragma once

#include <cmath>
#include <tuple>

#include "Vector2.h"

namespace Stellar::Native::Math::Vectors
{
    /// @brief Three-dimensional vector with arithmetic components.
    /// @tparam T Component type; must satisfy Concepts::Scalar
    ///         (std::is_arithmetic_v<T>).
    ///
    /// Components are stored in X/Y/Z order. Comparison is lexicographic
    /// (X, then Y, then Z), which gives a strict total order but is unrelated
    /// to geometric length. The type has no virtual functions and owns no
    /// resources.
    template <Scalar T>
    struct API Vector3 final
    {
        T X{}; ///< First (horizontal) component; defaults to zero.
        T Y{}; ///< Second (vertical) component; defaults to zero.
        T Z{}; ///< Third (depth) component; defaults to zero.

        // construction

        /// @brief Creates a zero vector (X = Y = Z = 0).
        constexpr explicit Vector3() noexcept = default;

        /// @brief Initializes the vector from a three-element C array.
        /// @param v Array whose first three elements become X, Y and Z.
        /// @pre @p v points to at least three elements of type @p T.
        /// @note The array is read once and is not retained.
        constexpr explicit Vector3(T v[3]) noexcept
            : X(v[0]),
              Y(v[1]),
              Z(v[2]) {}

        /// @brief Broadcasts one scalar to all three components.
        /// @param v Value copied into X, Y and Z.
        constexpr explicit Vector3(T v) noexcept
            : X{v},
              Y{v},
              Z{v} {}

        /// @brief Creates a vector from explicit components.
        /// @param x Value for X.
        /// @param y Value for Y.
        /// @param z Value for Z.
        constexpr Vector3(T x, T y, T z) noexcept
            : X{x},
              Y{y},
              Z{z} {}

        /// @brief Creates a vector from a 2D vector and a Z component.
        /// @param xy Vector supplying X and Y.
        /// @param z Value for Z.
        constexpr Vector3(Link<const Vector2<T>> xy, T z) noexcept
            : X{xy.X},
              Y{xy.Y},
              Z{z} {}

        // copy/move

        /// @brief Copies all components from another vector.
        constexpr Vector3(const Vector3&) noexcept = default;
        /// @brief Moves all components from another vector.
        /// @note Scalars are copied, so move and copy have identical behavior.
        constexpr Vector3(Vector3&&) noexcept = default;
        /// @brief Copies all components from another vector.
        /// @return Reference to this vector.
        constexpr Vector3& operator=(const Vector3&) noexcept = default;
        /// @brief Moves all components from another vector.
        /// @return Reference to this vector.
        constexpr Vector3& operator=(Vector3&&) noexcept = default;

        /// @brief Trivial destructor; the vector owns no resources.
        constexpr ~Vector3() noexcept = default;

        // spaceship

        /// @brief Lexicographic three-way comparison of X, then Y, then Z.
        /// @return Ordering result suitable for associative containers.
        /// @note This is not a length comparison.
        constexpr auto operator<=>(const Vector3&) const noexcept = default;

        // values

        /// @brief Returns all components as a tuple.
        /// @return Tuple containing (X, Y, Z).
        constexpr std::tuple<T, T, T> unpack() const { return std::make_tuple(X, Y, Z); }

        // math

        /// @brief Computes the dot product with another vector.
        /// @param o Other vector.
        /// @return X * o.X + Y * o.Y + Z * o.Z.
        [[nodiscard]] constexpr T dot(const Vector3& o) const noexcept
        {
            return X * o.X + Y * o.Y + Z * o.Z;
        }

        /// @brief Returns the squared Euclidean length.
        /// @return X * X + Y * Y + Z * Z, computed without a square root.
        [[nodiscard]] constexpr T LengthSq() const noexcept { return dot(*this); }

        /// @brief Returns the Euclidean length.
        /// @return sqrt(LengthSq()). For integral T the square root is computed
        ///         in double and converted back to T, truncating the fraction.
        [[nodiscard]] constexpr T Length() const noexcept { return std::sqrt(LengthSq()); }

        // cross

        /// @brief Computes the right-handed cross product.
        /// @param o Right-hand operand.
        /// @return Vector perpendicular to both operands, following the
        ///         right-hand rule; zero when the operands are parallel.
        [[nodiscard]] constexpr Vector3 cross(Link<const Vector3> o) const noexcept
        {
            return Vector3{
                Y * o.Z - Z * o.Y,
                Z * o.X - X * o.Z,
                X * o.Y - Y * o.X
            };
        }

        /// @brief Adds a scalar to each component.
        /// @param o Value added to X, Y and Z.
        /// @return New vector containing the sums.
        [[nodiscard]] constexpr Vector3 Add(T o) const noexcept { return Vector3{X + o, Y + o, Z + o}; }

        /// @brief Subtracts a scalar from each component.
        /// @param o Value subtracted from X, Y and Z.
        /// @return New vector containing the differences.
        [[nodiscard]] constexpr Vector3 Sub(T o) const noexcept { return Vector3{X - o, Y - o, Z - o}; }

        /// @brief Multiplies each component by a scalar.
        /// @param o Factor applied to X, Y and Z.
        /// @return New vector containing the products.
        [[nodiscard]] constexpr Vector3 Mul(T o) const noexcept { return Vector3{X * o, Y * o, Z * o}; }

        /// @brief Divides each component by a scalar.
        /// @param o Divisor applied to X, Y and Z.
        /// @return New vector containing the quotients.
        /// @note No zero check is performed; division by zero follows T's rules.
        [[nodiscard]] constexpr Vector3 Div(T o) const noexcept { return Vector3{X / o, Y / o, Z / o}; }

        /// @brief Adds another vector component-wise.
        /// @param o Vector whose components are added.
        /// @return New vector containing the sums.
        [[nodiscard]] constexpr Vector3 Add(Link<const Vector3> o) const noexcept
        {
            return Vector3{X + o.X, Y + o.Y, Z + o.Z};
        }

        /// @brief Subtracts another vector component-wise.
        /// @param o Vector whose components are subtracted.
        /// @return New vector containing the differences.
        [[nodiscard]] constexpr Vector3 Sub(Link<const Vector3> o) const noexcept
        {
            return Vector3{X - o.X, Y - o.Y, Z - o.Z};
        }

        /// @brief Multiplies by another vector component-wise.
        /// @param o Vector whose components are used as factors.
        /// @return New vector containing the products.
        [[nodiscard]] constexpr Vector3 Mul(Link<const Vector3> o) const noexcept
        {
            return Vector3{X * o.X, Y * o.Y, Z * o.Z};
        }

        /// @brief Divides by another vector component-wise.
        /// @param o Vector whose components are used as divisors.
        /// @return New vector containing the quotients.
        /// @note No zero check is performed on any component.
        [[nodiscard]] constexpr Vector3 Div(Link<const Vector3> o) const noexcept
        {
            return Vector3{X / o.X, Y / o.Y, Z / o.Z};
        }

        /// @brief Adds a scalar to all components (same as Add(o)).
        /// @param o Value added to each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector3 operator+(T o) const noexcept { return Add(o); }
        /// @brief Subtracts a scalar from all components (same as Sub(o)).
        /// @param o Value subtracted from each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector3 operator-(T o) const noexcept { return Sub(o); }
        /// @brief Multiplies all components by a scalar (same as Mul(o)).
        /// @param o Scale factor applied to each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector3 operator*(T o) const noexcept { return Mul(o); }
        /// @brief Divides all components by a scalar (same as Div(o)).
        /// @param o Divisor; must not be zero.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector3 operator/(T o) const noexcept { return Div(o); }

        /// @brief Adds another vector component-wise (same as Add(o)).
        /// @param o Vector to add.
        /// @return Component-wise sum.
        [[nodiscard]] constexpr Vector3 operator+(Link<const Vector3> o) const noexcept { return Add(o); }
        /// @brief Subtracts another vector component-wise (same as Sub(o)).
        /// @param o Vector to subtract.
        /// @return Component-wise difference.
        [[nodiscard]] constexpr Vector3 operator-(Link<const Vector3> o) const noexcept { return Sub(o); }
        /// @brief Multiplies by another vector component-wise (same as Mul(o)).
        /// @param o Vector to multiply by.
        /// @return Component-wise product.
        [[nodiscard]] constexpr Vector3 operator*(Link<const Vector3> o) const noexcept { return Mul(o); }
        /// @brief Divides by another vector component-wise (same as Div(o)).
        /// @param o Vector whose components are used as divisors.
        /// @return Component-wise quotient.
        [[nodiscard]] constexpr Vector3 operator/(Link<const Vector3> o) const noexcept { return Div(o); }

        /// @brief Returns the XY projection.
        /// @return Two-component vector (X, Y).
        [[nodiscard]] constexpr Vector2<T> xy() const noexcept { return {X, Y}; }
        /// @brief Returns the XZ projection.
        /// @return Two-component vector (X, Z).
        [[nodiscard]] constexpr Vector2<T> xz() const noexcept { return {X, Z}; }
        /// @brief Returns the YZ projection.
        /// @return Two-component vector (Y, Z).
        [[nodiscard]] constexpr Vector2<T> yz() const noexcept { return {Y, Z}; }

        // unary minus

        /// @brief Returns the negated vector (-X, -Y, -Z).
        /// @return Negated vector.
        [[nodiscard]] constexpr Vector3 operator-() const noexcept { return {-X, -Y, -Z}; }
    };
}
