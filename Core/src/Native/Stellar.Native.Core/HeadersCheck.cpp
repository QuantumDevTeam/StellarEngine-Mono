// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file HeadersCheck.cpp
/// @brief Compiles every public header of the core as C++20.
///
/// The project is C++23, its public headers must stay C++20: C++/CLI wrappers include them and /clr stops at C++20.
/// This file is built with /std:c++20 and without the precompiled header, so a C++23 feature in a header fails here.
#include "include/Kernel/kernel.h"
#include "include/Core/core.h"
#include "include/Core/core_contracts.h"

static_assert(__cplusplus == 202002L, "This file must be compiled as C++20");
