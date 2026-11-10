// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Math/Vector/Vector2.h
/// @brief Two-component arithmetic vector.
///
/// Vector2 is a lightweight value type used for positions, sizes, directions
/// and texture coordinates. Components are stored in X/Y order and arithmetic
/// is component-wise unless a method documents otherwise. Arithmetic
/// operations are constexpr and noexcept; division by zero is left to the
/// scalar type (undefined for integers, infinity/NaN for IEEE-754 floating
/// point).
///
/// @note This header relies on the kernel prelude for the Scalar concept and
///       the API macro. Include Kernel/kernel.h before this header when it is
///       used from a standalone translation unit.
#pragma once

#include <cmath>
#include <tuple>

namespace Stellar::Native::Math::Vectors
{
    /// @brief Two-dimensional vector with arithmetic components.
    /// @tparam T Component type; must satisfy Concepts::Scalar
    ///         (std::is_arithmetic_v<T>).
    ///
    /// The type is a plain value type with no virtual functions and no owning
    /// resources. X and Y are stored in declaration order. Comparison is
    /// lexicographic (X first, then Y), which gives a strict total order but is
    /// unrelated to geometric length.
    template <Scalar T>
    struct API Vector2 final
    {
        T X{}; ///< First (horizontal) component; defaults to zero.
        T Y{}; ///< Second (vertical) component; defaults to zero.

        // constructions

        /// @brief Creates a zero vector (X = 0, Y = 0).
        constexpr explicit Vector2() noexcept = default;

        /// @brief Initializes the vector from a two-element C array.
        /// @param v Array whose first two elements become X and Y.
        /// @pre @p v points to at least two elements of type @p T.
        /// @note The array is read once and is not retained.
        constexpr explicit Vector2(T v[2]) noexcept
            : X(v[0]),
              Y(v[1]) {}

        /// @brief Broadcasts one scalar to both components.
        /// @param v Value copied into X and Y.
        constexpr explicit Vector2(T v) noexcept
            : X(v),
              Y(v) {}

        /// @brief Creates a vector from explicit components.
        /// @param x Value for X.
        /// @param y Value for Y.
        constexpr Vector2(T x, T y) noexcept
            : X(x),
              Y(y) {}

        // copy/move

        /// @brief Copies both components from another vector.
        constexpr Vector2(const Vector2&) noexcept = default;
        /// @brief Moves both components from another vector.
        /// @note Scalars are copied, so move and copy have identical behavior.
        constexpr Vector2(Vector2&&) noexcept = default;
        /// @brief Copies both components from another vector.
        /// @return Reference to this vector.
        constexpr Vector2& operator=(const Vector2&) noexcept = default;
        /// @brief Moves both components from another vector.
        /// @return Reference to this vector.
        constexpr Vector2& operator=(Vector2&&) noexcept = default;

        /// @brief Trivial destructor; the vector owns no resources.
        constexpr ~Vector2() noexcept = default;

        // spaceship

        /// @brief Lexicographic three-way comparison of X, then Y.
        /// @return Ordering result suitable for associative containers.
        /// @note This is not a length comparison.
        constexpr auto operator<=>(const Vector2&) const noexcept = default;

        // values

        /// @brief Returns both components as a tuple.
        /// @return Tuple containing (X, Y).
        constexpr std::tuple<T, T> unpack() const { return std::make_tuple(X, Y); }

        // math

        /// @brief Computes the dot product with another vector.
        /// @param o Other vector.
        /// @return X * o.X + Y * o.Y.
        [[nodiscard]] constexpr T dot(Link<const Vector2> o) const noexcept { return X * o.X + Y * o.Y; }

        /// @brief Returns the squared Euclidean length.
        /// @return X * X + Y * Y, computed without a square root.
        [[nodiscard]] constexpr T LengthSq() const noexcept { return dot(*this); }

        /// @brief Returns the Euclidean length.
        /// @return sqrt(LengthSq()). For integral T the square root is computed
        ///         in double and converted back to T, truncating the fraction.
        [[nodiscard]] constexpr T Length() const noexcept { return std::sqrt(LengthSq()); }

        /// @brief Adds a scalar to each component.
        /// @param o Value added to X and Y.
        /// @return New vector containing the sums.
        [[nodiscard]] constexpr Vector2 Add(T o) const noexcept { return Vector2{X + o, Y + o}; }

        /// @brief Subtracts a scalar from each component.
        /// @param o Value subtracted from X and Y.
        /// @return New vector containing the differences.
        [[nodiscard]] constexpr Vector2 Sub(T o) const noexcept { return Vector2{X - o, Y - o}; }

        /// @brief Multiplies each component by a scalar.
        /// @param o Factor applied to X and Y.
        /// @return New vector containing the products.
        [[nodiscard]] constexpr Vector2 Mul(T o) const noexcept { return Vector2{X * o, Y * o}; }

        /// @brief Divides each component by a scalar.
        /// @param o Divisor applied to X and Y.
        /// @return New vector containing the quotients.
        /// @note No zero check is performed; division by zero follows T's rules.
        [[nodiscard]] constexpr Vector2 Div(T o) const noexcept { return Vector2{X / o, Y / o}; }

        /// @brief Adds another vector component-wise.
        /// @param o Vector whose components are added.
        /// @return New vector containing the sums.
        [[nodiscard]] constexpr Vector2 Add(Link<const Vector2> o) const noexcept { return Vector2{X + o.X, Y + o.Y}; }

        /// @brief Subtracts another vector component-wise.
        /// @param o Vector whose components are subtracted.
        /// @return New vector containing the differences.
        [[nodiscard]] constexpr Vector2 Sub(Link<const Vector2> o) const noexcept { return Vector2{X - o.X, Y - o.Y}; }

        /// @brief Multiplies by another vector component-wise.
        /// @param o Vector whose components are used as factors.
        /// @return New vector containing the products.
        [[nodiscard]] constexpr Vector2 Mul(Link<const Vector2> o) const noexcept { return Vector2{X * o.X, Y * o.Y}; }

        /// @brief Divides by another vector component-wise.
        /// @param o Vector whose components are used as divisors.
        /// @return New vector containing the quotients.
        /// @note No zero check is performed on either component.
        [[nodiscard]] constexpr Vector2 Div(Link<const Vector2> o) const noexcept { return Vector2{X / o.X, Y / o.Y}; }

        /// @brief Adds a scalar to both components (same as Add(o)).
        /// @param o Value added to each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector2 operator+(T o) const noexcept { return Add(o); }
        /// @brief Subtracts a scalar from both components (same as Sub(o)).
        /// @param o Value subtracted from each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector2 operator-(T o) const noexcept { return Sub(o); }
        /// @brief Multiplies both components by a scalar (same as Mul(o)).
        /// @param o Scale factor applied to each component.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector2 operator*(T o) const noexcept { return Mul(o); }
        /// @brief Divides both components by a scalar (same as Div(o)).
        /// @param o Divisor; must not be zero.
        /// @return New vector with the scalar applied.
        [[nodiscard]] constexpr Vector2 operator/(T o) const noexcept { return Div(o); }

        /// @brief Adds another vector component-wise (same as Add(o)).
        /// @param o Vector to add.
        /// @return Component-wise sum.
        [[nodiscard]] constexpr Vector2 operator+(Link<const Vector2> o) const noexcept { return Add(o); }
        /// @brief Subtracts another vector component-wise (same as Sub(o)).
        /// @param o Vector to subtract.
        /// @return Component-wise difference.
        [[nodiscard]] constexpr Vector2 operator-(Link<const Vector2> o) const noexcept { return Sub(o); }
        /// @brief Multiplies by another vector component-wise (same as Mul(o)).
        /// @param o Vector to multiply by.
        /// @return Component-wise product.
        [[nodiscard]] constexpr Vector2 operator*(Link<const Vector2> o) const noexcept { return Mul(o); }
        /// @brief Divides by another vector component-wise (same as Div(o)).
        /// @param o Vector whose components are used as divisors.
        /// @return Component-wise quotient.
        [[nodiscard]] constexpr Vector2 operator/(Link<const Vector2> o) const noexcept { return Div(o); }

        // unary minus

        /// @brief Returns the negated vector (-X, -Y).
        /// @return Negated vector.
        [[nodiscard]] constexpr Vector2 operator-() const noexcept { return Vector2{-X, -Y}; }
    };
}
