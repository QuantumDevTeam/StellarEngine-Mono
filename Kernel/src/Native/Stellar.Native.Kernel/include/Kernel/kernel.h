// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/kernel.h
/// @brief Umbrella header for the kernel vocabulary types and helpers.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include <cstddef>
#include <cstdint>
#include <utility>
#include <memory>
#include <type_traits>

// simple headers
#include "macro.h"
#include "Types.h"
#include "Concepts.h"
#include "Helpers.h"
#include "abi.h"

using namespace Stellar::Native; // NOLINT
using namespace Stellar::Native::Types; // NOLINT
using namespace Stellar::Native::Concepts; // NOLINT

/// @brief Root namespace of the Stellar engine.
namespace Stellar {}

/// @brief Native (unmanaged C++) implementation of the engine.
namespace Stellar::Native {}

/// @brief Engine kernel: contracts, math, graphics, platform, model and video.
namespace Stellar::Native::Kernel
{
    /// @brief Major version of the kernel API.
    inline constexpr uint32_t kKernelVersionMajor = 0;

    /// @brief Minor version of the kernel API.
    inline constexpr uint32_t kKernelVersionMinor = 2;
}
