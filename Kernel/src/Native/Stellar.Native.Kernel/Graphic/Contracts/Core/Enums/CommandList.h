// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>

/// @file Graphic/Contracts/Core/Enums/CommandList.h
/// @brief Command-list kinds and GPU query types.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Declares which command families a command list may record.
    ///
    /// The backend uses the usage to select the native command-list type. A
    /// list created for one usage must not receive commands from another
    /// family; for example, draw calls are invalid on a Copy list.
    enum class API CommandListUsage : uint32_t
    {
        Direct       = 0,    ///< General-purpose list: draws, dispatches, copies and state changes.
        Bundle       = 1,    ///< Pre-recorded list that can be executed repeatedly inside a direct list.
        Compute      = 2,    ///< Compute-only list (dispatches, UAV barriers and copies).
        Copy         = 3,    ///< Copy/transfer-only list.
        VideoDecode  = 4,    ///< Video decode commands.
        VideoProcess = 5,    ///< Video processing commands.
        VideoEncode  = 6,    ///< Video encode commands.
    };

    /// @brief GPU query kinds that can be recorded between BeginQuery/EndQuery.
    enum class API QueryType : uint32_t
    {
        Occlusion = 0,      ///< Number of samples that passed the depth/stencil test.
        BinaryOcclusion,    ///< 1 when at least one sample passed, otherwise 0.
        Timestamp,          ///< GPU timestamp in device ticks (see AdapterInfo::timestampFrequency).
        Event,              // COMMENT
    };
}
