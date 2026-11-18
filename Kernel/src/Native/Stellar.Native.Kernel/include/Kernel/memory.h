// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/memory.h
/// @brief Handle of native memory in C++: native lists over Abi/Memory.h. Include it after Kernel/kernel.h.
///
/// It pulls in <atomic>: a C++/CLI translation unit does not include it.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include "../../Memory/NativeList.h"
