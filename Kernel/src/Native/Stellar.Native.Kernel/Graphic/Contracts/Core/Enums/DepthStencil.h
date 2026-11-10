// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/DepthStencil.h
/// @brief Depth/stencil comparison and stencil-update operations.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Comparison performed between a new fragment value and the stored value.
    ///
    /// The comparison is used for depth testing and for stencil testing. The
    /// fragment passes when the comparison evaluates to true.
    enum class API CompareOp : uint32_t
    {
        Never,        ///< Always fails.
        Less,         ///< new < stored.
        Equal,        ///< new == stored.
        LessEqual,    ///< new <= stored (default depth test).
        Greater,      ///< new > stored.
        NotEqual,     ///< new != stored.
        GreaterEqual, ///< new >= stored.
        Always,       ///< Always passes.
    };

    /// @brief Action applied to a stencil buffer value when a test succeeds or fails.
    enum class API StencilOp : uint32_t
    {
        Keep,              ///< Keep the current value.
        Zero,              ///< Set the value to zero.
        Replace,           ///< Replace with the stencil reference value.
        IncrementSaturate, ///< Increment, clamping at the maximum unsigned value.
        DecrementSaturate, ///< Decrement, clamping at zero.
        Invert,            ///< Bitwise invert the value.
        IncrementWrap,     ///< Increment, wrapping to zero at the maximum value.
        DecrementWrap,     ///< Decrement, wrapping to the maximum value at zero.
    };
}
