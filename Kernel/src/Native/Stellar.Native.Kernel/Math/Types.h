// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Math/Types.h
/// @brief Forward declarations for the Math vectors and shapes.
///
/// This definition-free header lets interfaces mention Math value types without
/// pulling in the complete definitions. Include the concrete header (or
/// Kernel/math.h) when a complete type is required.
///
/// @note The declarations use the Scalar concept, so this header relies on the
///       kernel prelude. Include Kernel/kernel.h before it when it is used from
///       a standalone translation unit.
///
/// @note The declarations themselves are intentionally not documented here.
///       Doxygen merges comments on a forward declaration with comments on the
///       definition, which duplicates the brief and template parameters. The
///       symbols are documented in their defining headers.
#pragma once

#include <variant>

/// @brief Math value types used across the engine.
namespace Stellar::Native::Math
{
    /// @brief Two-dimensional geometry value types.
    namespace Shapes
    {
        // Forward declaration; the definition is documented in
        // Math/Shapes/Rect.h.
        template <Scalar T>
        struct API Rect;

        // Forward declaration; the definition is documented in
        // Math/Shapes/ScissorRect.h.
        template <Scalar T>
        struct API ScissorRect;
    }

    /// @brief Vector value types.
    namespace Vectors
    {
        // Forward declaration; the definition is documented in
        // Math/Vector/Vector2.h.
        template <Scalar T>
        struct API Vector2;

        // Forward declaration; the definition is documented in
        // Math/Vector/Vector3.h.
        template <Scalar T>
        struct API Vector3;

        // Forward declaration; the definition is documented in
        // Math/Vector/Vector4.h.
        template <Scalar T>
        struct API Vector4;

        template <Scalar T>
        using Vector = std::variant<
            std::monostate,
            Vector2<T>,
            Vector3<T>,
            Vector4<T>
        >;
    }
}
