// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once

#include <cstdint>

#include "../include/Kernel/macro.h"
#include "../include/Kernel/Types.h"

/// @file Video/Types.h
/// @brief Forward declarations of the virtual-camera video types.

/// @brief Virtual camera producer API.
namespace Stellar::Native::Kernel::Video
{
    /// @brief One virtual camera device: streaming, format negotiation and lifetime.
    class API IVirtualCamera;

    /// @brief Creates and enumerates virtual camera devices.
    class API IVirtualCameraFactory;

    /// @brief One captured video frame with its pixel data and timing.
    struct API VideoFrame;

    /// @brief Virtual camera creation parameters (name, format, resolution, fps).
    struct API VirtualCameraDesc;

    /// @brief Runtime counters for a virtual camera (frames, drops, ...).
    struct API VirtualCameraStats;

    /// @brief Who may open the camera (application, system, ...).
    enum class API CameraAccess : uint32_t;

    /// @brief How long the camera lives after its last consumer disconnects.
    enum class API CameraLifetime : uint32_t;

    /// @brief Pixel layout of a VideoFrame.
    enum class API PixelFormat : uint32_t;
}
