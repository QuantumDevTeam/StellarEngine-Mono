// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Core/core_contracts.h
/// @brief Umbrella of the native core for native consumers. Include it after Core/core.h.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

#include "core_interop.h"

#include "Data/Collections/ICollection.h"
#include "Data/Collections/ConcurrentUnorderedMap.h"
#include "Data/Collections/ConcurrentPriorityQueue.h"
#include "Data/Collections/ConcurrentVector.h"
#include "Data/Collections/DataContainer.h"
#include "Data/Collections/DoubleBufferedSnapshotMap.h"
#include "Data/Collections/HandlePool.h"

#include "Failures/FailureLevel.h"
