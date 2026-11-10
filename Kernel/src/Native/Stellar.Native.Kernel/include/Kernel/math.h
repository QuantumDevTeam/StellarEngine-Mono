// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/math.h
/// @brief Umbrella include for the Math value types.
///
/// Pulls in the vector types and the axis-aligned rectangles. The header only
/// provides definitions: it does not import the Math namespaces and does not
/// include the kernel prelude. Include Kernel/kernel.h before this header when
/// compiling a standalone translation unit, because the Math headers rely on
/// the Scalar concept and the API macro.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include "../../Math/Types.h"

// vectors
#include "../../Math/Vector/Vector2.h"
#include "../../Math/Vector/Vector3.h"
#include "../../Math/Vector/Vector4.h"

// rectangles
#include "../../Math/Shapes/Rect.h"
#include "../../Math/Shapes/ScissorRect.h"
