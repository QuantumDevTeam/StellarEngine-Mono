// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Math/Shapes/ScissorRect.h
/// @brief Rectangle used for scissor and clip regions.
///
/// ScissorRect stores the four native edges directly (Left, Top, Right,
/// Bottom). It is passed to ICommandList::SetScissor() and is mapped verbatim
/// to D3D11_RECT/D3D12_RECT by the backends. No normalization, coordinate
/// conversion or intersection test is performed by this type.
///
/// @note Native scissor APIs interpret Right and Bottom as exclusive bounds,
///       so the covered area is [Left, Right) x [Top, Bottom). The type does
///       not enforce this convention; callers must supply valid edges with
///       Right >= Left and Bottom >= Top.
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
    /// @brief Scissor rectangle with arithmetic edge coordinates.
    /// @tparam T Component type; must satisfy Concepts::Scalar
    ///         (std::is_arithmetic_v<T>).
    ///
    /// The type is a plain value type used for clipping. Comparison is
    /// lexicographic (Left, Top, Right, Bottom), not spatial.
    template <Scalar T>
    struct API ScissorRect final
    {
        T Left = 0;   ///< Left edge; inclusive in native scissor APIs.
        T Top = 0;    ///< Top edge; inclusive in native scissor APIs.
        T Right = 0;  ///< Right edge; exclusive in native scissor APIs.
        T Bottom = 0; ///< Bottom edge; exclusive in native scissor APIs.

        // construction

        /// @brief Creates an empty rectangle at the origin (all edges zero).
        constexpr explicit ScissorRect() noexcept = default;

        /// @brief Creates a rectangle from explicit single value.
        /// @param v initial value
        constexpr explicit ScissorRect(T v) noexcept
            : Left{v},
              Top{v},
              Right{v},
              Bottom{v} {}

        /// @brief Creates a rectangle from explicit edge coordinates.
        /// @param left Left edge.
        /// @param top Top edge.
        /// @param right Right edge.
        /// @param bottom Bottom edge.
        constexpr ScissorRect(T left, T top, T right, T bottom) noexcept
            : Left{left},
              Top{top},
              Right{right},
              Bottom{bottom} {}

        /// @brief Creates a rectangle from two corner vectors.
        /// @param p1 Vector supplying Left and Top; uses integer components.
        /// @param p2 Vector supplying Right and Bottom.
        constexpr ScissorRect(Link<const Vectors::Vector2<T>> p1, Link<const Vectors::Vector2<T>> p2) noexcept
            : Left{p1.X},
              Top{p1.Y},
              Right{p2.X},
              Bottom{p2.Y} {}

        /// @brief Creates a rectangle from a packed vector.
        /// @param v Vector whose components map to Left, Top, Right and Bottom.
        constexpr explicit ScissorRect(Link<const Vectors::Vector4<T>> v) noexcept
            : Left{v.X},
              Top{v.Y},
              Right{v.Z},
              Bottom{v.W} {}

        // copy/move

        /// @brief Copies all edges from another rectangle.
        ScissorRect(const ScissorRect&) noexcept = default;
        /// @brief Moves all edges from another rectangle.
        /// @note Scalars are copied, so move and copy have identical behavior.
        ScissorRect(ScissorRect&&) noexcept = default;
        /// @brief Copies all edges from another rectangle.
        /// @return Reference to this rectangle.
        ScissorRect& operator=(const ScissorRect&) noexcept = default;
        /// @brief Moves all edges from another rectangle.
        /// @return Reference to this rectangle.
        ScissorRect& operator=(ScissorRect&&) noexcept = default;

        /// @brief Trivial destructor; the rectangle owns no resources.
        ~ScissorRect() noexcept = default;

        // spaceship

        /// @brief Lexicographic three-way comparison of Left, Top, Right, then Bottom.
        /// @return Ordering result suitable for associative containers.
        /// @note This is not a spatial ordering.
        constexpr auto operator<=>(const ScissorRect&) const noexcept = default;

        // values

        /// @brief Returns all edges as a tuple.
        /// @return Tuple containing (Left, Top, Right, Bottom).
        constexpr std::tuple<T, T, T, T> unpack() const { return std::make_tuple(Left, Top, Right, Bottom); }

        /// @brief Returns all edges as a braced list (currently ill-formed).
        /// @return Braced initializer list containing Left, Top, Right and Bottom.
        /// @warning The return type is deduced with auto from a braced-init-list,
        ///          which C++ does not allow. Instantiating arr() fails to
        ///          compile; use unpack() instead.
        constexpr auto arr() const noexcept { return {Left, Top, Right, Bottom}; }

        // TODO: add other methods
    };
}
