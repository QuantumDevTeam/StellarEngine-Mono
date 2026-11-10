// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/platform.h
/// @brief Lightweight platform forward declarations and namespace import.
///
/// This header exposes Platform/Types.h, which contains forward declarations
/// only. It is suitable for code that just needs platform type names. Include
/// platform_contracts.h to get the full core, event, window and backend
/// contract surface.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include "../../Platform/Types.h"

using namespace Stellar::Native::Kernel::Platform; // NOLINT
