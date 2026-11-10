// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Math/Shapes/Rect.h
/// @brief Axis-aligned rectangle defined by an origin and extents.
///
/// Rect stores an X/Y origin together with Width/Height and is used for window
/// rectangles, viewports, UI layout and other axis-aligned regions. The type is
/// a plain value type: it performs no validation and does not own resources.
///
/// @note The edge helpers follow a Y-up convention: getTop() returns
///       Y + Height and getBottom() returns Y. Native screen coordinates on
///       Windows use Y-down, so the visually top edge is getBottom() there.
///       The stored values themselves are never converted.
///
/// @note This header relies on the kernel prelude for the Scalar concept and
///       the API macro. Include Kernel/kernel.h before this header when it is
///       used from a standalone translation unit.
#pragma once

#include <tuple>

#include "Math/Vector/Vector2.h"
#include "Math/Vector/Vector4.h"

namespace Stellar::Native::Math::Shapes
{
    /// @brief Axis-aligned rectangle with arithmetic components.
    /// @tparam T Component type; must satisfy Concepts::Scalar
    ///         (std::is_arithmetic_v<T>).
    ///
    /// Width and Height are expected to be non-negative; the geometry helpers
    /// produce meaningful results only for non-negative extents. Comparison is
    /// lexicographic (X, Y, Width, Height), not spatial.
    template <Scalar T>
    struct API Rect final
    {
        T X = 0;      ///< Horizontal origin (left edge under a Y-up convention).
        T Y = 0;      ///< Vertical origin (bottom edge under a Y-up convention).
        T Width = 0;  ///< Horizontal extent; expected to be non-negative.
        T Height = 0; ///< Vertical extent; expected to be non-negative.

        // construction

        /// @brief Creates an empty rectangle at the origin (all fields zero).
        constexpr explicit Rect() noexcept = default;

        /// @brief Creates a rectangle from explicit single value.
        /// @param v initial value
        constexpr explicit Rect(T v) noexcept
            : X{v},
              Y{v},
              Width{v},
              Height{v} {}

        /// @brief Creates a rectangle from explicit position and size.
        /// @param x Horizontal origin.
        /// @param y Vertical origin.
        /// @param width Horizontal extent.
        /// @param height Vertical extent.
        constexpr Rect(T x, T y, T width, T height) noexcept
            : X{x},
              Y{y},
              Width{width},
              Height{height} {}

        /// @brief Creates a rectangle from a position and a size vector.
        /// @param pos Position vector; its components become X and Y.
        /// @param size Size vector; its components become Width and Height.
        /// @note The position uses integer components regardless of @p T,
        ///       matching native window coordinates.
        constexpr Rect(Link<const Vectors::Vector2<T>> pos, Link<const Vectors::Vector2<T>> size) noexcept
            : X{pos.X},
              Y{pos.Y},
              Width{size.X},
              Height{size.Y} {}

        /// @brief Creates a rectangle from a packed vector.
        /// @param v Vector whose components map to X, Y, Width and Height.
        constexpr explicit Rect(Link<const Vectors::Vector4<T>> v) noexcept
            : X{v.X},
              Y{v.Y},
              Width{v.Z},
              Height{v.W} {}

        // copy/move

        /// @brief Copies all fields from another rectangle.
        Rect(const Rect&) noexcept = default;
        /// @brief Moves all fields from another rectangle.
        /// @note Scalars are copied, so move and copy have identical behavior.
        Rect(Rect&&) noexcept = default;
        /// @brief Copies all fields from another rectangle.
        /// @return Reference to this rectangle.
        Rect& operator=(const Rect&) noexcept = default;
        /// @brief Moves all fields from another rectangle.
        /// @return Reference to this rectangle.
        Rect& operator=(Rect&&) noexcept = default;

        /// @brief Trivial destructor; the rectangle owns no resources.
        ~Rect() noexcept = default;

        // spaceship

        /// @brief Lexicographic three-way comparison of X, Y, Width, then Height.
        /// @return Ordering result suitable for associative containers.
        /// @note This is not a spatial ordering.
        constexpr auto operator<=>(const Rect&) const noexcept = default;

        // values

        /// @brief Returns all fields as a tuple.
        /// @return Tuple containing (X, Y, Width, Height).
        constexpr std::tuple<T, T, T, T> unpack() const { return std::make_tuple(X, Y, Width, Height); }

        /// @brief Returns all fields as a braced list (currently ill-formed).
        /// @return Braced initializer list containing X, Y, Width and Height.
        /// @warning The return type is deduced with auto from a braced-init-list,
        ///          which C++ does not allow. Instantiating arr() fails to
        ///          compile; use unpack() instead.
        constexpr auto arr() const noexcept { return {X, Y, Width, Height}; }

        // scissor edges

        /// @brief Returns the left edge (X).
        /// @return X coordinate.
        [[nodiscard]] constexpr T getLeft() const noexcept { return X; }

        /// @brief Returns the right edge (X + Width).
        /// @return X + Width.
        [[nodiscard]] constexpr T getRight() const noexcept { return X + Width; }

        /// @brief Returns the top edge (Y + Height) under a Y-up convention.
        /// @return Y + Height.
        /// @note With Y-down screen coordinates this value is the visually
        ///       bottom edge; no conversion is applied.
        [[nodiscard]] constexpr T getTop() const noexcept { return Y + Height; }

        /// @brief Returns the bottom edge (Y) under a Y-up convention.
        /// @return Y coordinate.
        /// @note With Y-down screen coordinates this value is the visually
        ///       top edge; no conversion is applied.
        [[nodiscard]] constexpr T getBottom() const noexcept { return Y; }

        // position / size

        /// @brief Returns the origin as a vector.
        /// @return Two-component vector (X, Y).
        [[nodiscard]] constexpr Vectors::Vector2<T> getPos() const noexcept { return {X, Y}; }

        /// @brief Returns the extents as a vector.
        /// @return Two-component vector (Width, Height).
        [[nodiscard]] constexpr Vectors::Vector2<T> getSize() const noexcept { return {Width, Height}; }

        /// @brief Returns the center point of the rectangle.
        /// @return Vector (X + Width / 2, Y + Height / 2).
        /// @note For integral T the division truncates toward zero, so the
        ///       center falls on a cell boundary rather than between cells.
        [[nodiscard]] constexpr Vectors::Vector2<T> getCenter() const noexcept
        {
            return Vectors::Vector2<T>{X + Width / T{2}, Y + Height / T{2}};
        }

        /// @brief Sets the origin from explicit coordinates.
        /// @param x New X value.
        /// @param y New Y value.
        constexpr void setPos(T x, T y) noexcept
        {
            X = x;
            Y = y;
        }

        /// @brief Sets the origin from a vector.
        /// @param p New position; its components are copied into X and Y.
        constexpr void setPos(Link<const Vectors::Vector2<T>> p) noexcept
        {
            X = p.X;
            Y = p.Y;
        }

        /// @brief Sets the extents from explicit values.
        /// @param width New Width value.
        /// @param height New Height value.
        constexpr void setSize(T width, T height) noexcept
        {
            Width = width;
            Height = height;
        }

        /// @brief Sets the extents from a vector.
        /// @param s New size; its components are copied into Width and Height.
        constexpr void setSize(Link<const Vectors::Vector2<T>> s) noexcept
        {
            Width = s.X;
            Height = s.Y;
        }

        // queries

        /// @brief Tests whether a point lies inside the rectangle.
        /// @param p Point to test.
        /// @return True when X <= p.X < X + Width and Y <= p.Y < Y + Height.
        /// @note The rectangle is half-open: the minimum edges are inside, the
        ///       maximum edges are outside.
        [[nodiscard]] constexpr bool Contains(Link<const Vectors::Vector2<T>> p) const noexcept
        {
            return p.X >= X && p.X < X + Width
                   && p.Y >= Y && p.Y < Y + Height;
        }

        /// @brief Tests whether two rectangles overlap.
        /// @param o Other rectangle.
        /// @return True when the rectangles share a region with positive area.
        /// @note Rectangles that only touch along an edge or at a corner do not
        ///       intersect.
        [[nodiscard]] constexpr bool Intersects(const Rect& o) const noexcept
        {
            return !(o.X >= X + Width || o.X + o.Width <= X
                     || o.Y >= Y + Height || o.Y + o.Height <= Y);
        }
    };
}
