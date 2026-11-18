// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Abi/Profiling.h
/// @brief The event of a profiler, as C# and C++ write it.
///
/// The profiler is a module. It keeps the events of each thread in a native list (Abi/Memory.h) of StellarProfilerEvent,
/// so events written by C# and by native code go into the same buffers, on one clock, without a conversion.
#ifndef STELLAR_KERNEL_ABI_PROFILING_H
#define STELLAR_KERNEL_ABI_PROFILING_H

#include "Abi/Abi.h"

/// @brief What a profiler event says. Mirrors `Stellar.Kernel.Profiling.ProfilerEventKind`.
enum
{
    /// @brief A marked span of work starts on this thread.
    STELLAR_PROFILER_BEGIN = 0,

    /// @brief The last span of the same marker that started on this thread ends.
    STELLAR_PROFILER_END = 1,

    /// @brief Something happened at one moment.
    STELLAR_PROFILER_INSTANT = 2,

    /// @brief A counter has a value.
    STELLAR_PROFILER_COUNTER = 3,

    /// @brief A frame of a loop starts: the marker is the loop, the value its frame index.
    STELLAR_PROFILER_FRAME = 4
};

/// @brief One event of a profiler. Mirrors `Stellar.Kernel.Profiling.ProfilerEvent`.
using StellarProfilerEvent = struct StellarProfilerEvent
{
    /// @brief When, in ticks of the high-resolution clock of the platform: QueryPerformanceCounter on Windows,
    /// CLOCK_MONOTONIC in nanoseconds elsewhere, which is what Stopwatch.GetTimestamp reads in C#.
    int64_t timestamp;

    /// @brief The number of the marker, the loop or the counter, as `Stellar.Kernel.Profiling.ProfilerMarkers` gives
    /// it. 0 is no marker.
    int32_t marker;

    /// @brief What happened: STELLAR_PROFILER_BEGIN and the others.
    uint16_t kind;

    /// @brief Zero.
    uint16_t reserved;

    /// @brief The value of a counter, the index of a frame, or what an instant carries; 0 otherwise.
    int64_t value;
};

#ifdef __cplusplus
static_assert(sizeof(StellarProfilerEvent) == 24, "StellarProfilerEvent layout is part of the ABI");
#endif

#endif
