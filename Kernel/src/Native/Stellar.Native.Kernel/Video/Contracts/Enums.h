// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once

#include "../Types.h"

/// @file Video/Contracts/Enums.h
/// @brief Pixel formats, lifetime and access enumerations for virtual cameras.

namespace Stellar::Native::Kernel::Video
{
    /// @brief Pixel layout of a VideoFrame buffer.
    enum class API PixelFormat : uint32_t
    {
        Unknown = 0, ///< Format is not specified.
        Bgra8   = 1,   ///< 8 bits per channel, blue/green/red/alpha byte order.
        Rgba8   = 2,   ///< 8 bits per channel, red/green/blue/alpha byte order.
    };

    /// @brief How long a virtual camera remains registered.
    enum class API CameraLifetime : uint32_t
    {
        Session = 0, ///< Camera disappears when the producing session ends.
        System  = 1,  ///< Camera is installed system-wide and persists across sessions.
    };

    /// @brief Which users may open the virtual camera.
    enum class API CameraAccess : uint32_t
    {
        CurrentUser = 0, ///< Only the user running the producer may open it.
        AllUsers    = 1,    ///< Any user on the machine may open it.
    };
}
