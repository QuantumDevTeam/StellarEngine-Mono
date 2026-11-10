// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once

#include <cstddef>
#include <cstdint>
#include <string>
#include <type_traits>

#include "../Types.h"
#include "Enums.h"

/// @file Video/Contracts/Descriptions.h
/// @brief Frame, virtual-camera and statistics descriptions.

namespace Stellar::Native::Kernel::Video
{
    /// @brief A single producer frame.
    ///
    /// The producer may use any row pitch that is at least `Width * 4`, but
    /// tightly packed BGRA8 frames take the fast path. The frame is trivially
    /// copyable because it crosses the producer/consumer boundary.
    struct API VideoFrame final
    {
        /// @brief Pointer to the first pixel byte; owned by the caller.
        Ptr<const uint8_t> Data = nullptr;

        /// @brief Size of the pixel buffer in bytes.
        size_t Size = 0;

        /// @brief Frame width in pixels.
        uint32_t Width = 0;

        /// @brief Frame height in pixels.
        uint32_t Height = 0;

        /// @brief Distance between the start of two consecutive rows in bytes.
        uint32_t Stride = 0;

        /// @brief Pixel layout of the buffer.
        PixelFormat Format = PixelFormat::Unknown;

        /// @brief Presentation timestamp in nanoseconds.
        int64_t TimestampNs = 0;
    };

    static_assert(std::is_trivially_copyable_v<VideoFrame>,
                  "VideoFrame is passed across the producer/consumer boundary and must stay trivial");

    /// @brief Virtual camera creation parameters.
    struct API VirtualCameraDesc final
    {
        /// @brief Friendly name shown by Windows and capture applications.
        std::wstring Name = L"Stellar Virtual Camera";

        /// @brief Stable producer identity.
        ///
        /// The shared mapping name is derived from it, so it must remain
        /// constant for the lifetime of the camera.
        std::wstring MappingId = L"StellarVirtualCamera";

        /// @brief Output width in pixels.
        uint32_t Width = 1920;

        /// @brief Output height in pixels.
        uint32_t Height = 1080;

        /// @brief Frame-rate numerator (frames per FpsDenominator seconds).
        uint32_t FpsNumerator = 30;

        /// @brief Frame-rate denominator.
        uint32_t FpsDenominator = 1;

        /// @brief Number of shared-memory frame slots.
        uint32_t SlotCount = 3;

        /// @brief Pixel layout produced by the camera.
        PixelFormat Format = PixelFormat::Bgra8;

        /// @brief How long the camera remains registered.
        CameraLifetime Lifetime = CameraLifetime::Session;

        /// @brief Which users may open the camera.
        CameraAccess Access = CameraAccess::CurrentUser;
    };

    /// @brief Runtime counters for a virtual camera.
    struct API VirtualCameraStats final
    {
        /// @brief Frames accepted from the producer.
        uint64_t SubmittedFrames = 0;

        /// @brief Frames rejected because no shared slot was available.
        uint64_t DroppedFrames = 0;

        /// @brief Frames that replaced an unread slot.
        uint64_t OverwrittenFrames = 0;

        /// @brief Frames consumed by the reader.
        uint64_t ReaderFrames = 0;

        /// @brief Sequence number of the most recent published frame.
        uint64_t LatestSequence = 0;

        /// @brief True while the camera is started.
        bool Running = false;

        /// @brief True while a consumer mapping is connected.
        bool Linked = false;

        /// @brief True while a reader has attached to the shared memory.
        bool ReaderAttached = false;
    };
}
