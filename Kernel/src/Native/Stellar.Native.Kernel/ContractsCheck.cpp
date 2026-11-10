// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file ContractsCheck.cpp
/// @brief Compiles every contract header of the kernel.
///
/// The kernel is header-only, so without this file a broken contract is noticed only by the modules that use it.
#include "pch.h"

#include "include/Kernel/math.h"
#include "include/Kernel/platform.h"
#include "include/Kernel/platform_contracts.h"
#include "include/Kernel/graphic.h"
#include "include/Kernel/graphic_contracts.h"
#include "include/Kernel/video.h"
#include "include/Kernel/video_contracts.h"

#include "Resources/ResourceContainer.h"
