// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/graphic.h
/// @brief Lightweight graphics value types and namespace import.
///
/// This header exposes Viewport, Color4 and the forward declarations from
/// Graphic/Types.h without pulling in the full contract surface. Applications
/// that create devices should include graphic_contracts.h instead.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include "../../Graphic/Types.h"
#include "../../Graphic/Viewport.h"
#include "../../Graphic/Color4.h"

using namespace Stellar::Native::Kernel::Graphics; // NOLINT
