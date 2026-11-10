// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Core/core_interop.h
/// @brief Umbrella for the wrappers: the classes managed code reaches.
///
/// Everything here is C++20 and pulls in no threading header, so a C++/CLI translation unit (/clr) may include it.
/// Include it after Kernel/kernel.h and Core/core.h.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include "Identifier.h"
#include "Label.h"

#include "Data/Registry/IdentifierRegistry.h"
#include "Data/Registry/LabelRegistry.h"
#include "Data/Registry/ContainerRegistry.h"

#include "Failures/NativeException.h"
#include "Failures/Guard.h"

#include "Modules/ModuleRegistry.h"

#include "CoreContext.h"
