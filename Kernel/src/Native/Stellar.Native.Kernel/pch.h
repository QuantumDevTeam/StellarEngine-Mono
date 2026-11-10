// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file pch.h
/// @brief Precompiled header with the standard-library includes used by the kernel.
#ifndef PCH_H
#define PCH_H

#pragma warning(disable: 4068) // unknown pragma (for clang)
#pragma warning(disable: 4003) // unspecified attribute in macros

// most used lib's
#include <cstddef>
#include <cstdint>
#include <utility>
#include <memory>
#include <type_traits>

// platform
// anti-pattern

// additional
// anti-pattern

// custom directives
#include "include/Kernel/kernel.h"

#endif
