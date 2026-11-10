// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once

#include "../Types.h"
#include "Descriptions.h"

/// @file Video/Contracts/Interfaces.h
/// @brief Producer-side virtual camera interfaces.

namespace Stellar::Native::Kernel::Video
{
    /// @brief Producer-side virtual camera.
    ///
    /// All methods are expected to be called from one producer thread.
    /// SubmitFrame never allocates and never blocks on the consumer process.
    class API IVirtualCamera // NOLINT
    {
    public:
        /// @brief Virtual destructor for safe destruction through the interface.
        virtual ~IVirtualCamera() = default;

        /// @brief Checks whether the camera was created successfully.
        /// @return True when the camera object is usable.
        [[nodiscard]] virtual bool IsValid() const noexcept = 0;

        /// @brief Checks whether the camera is started.
        /// @return True between a successful Start and Stop.
        [[nodiscard]] virtual bool IsRunning() const noexcept = 0;

        /// @brief Checks whether a consumer mapping is connected.
        /// @return True when the camera is linked to its consumer.
        [[nodiscard]] virtual bool IsLinked() const noexcept = 0;

        /// @brief Returns the immutable creation parameters.
        /// @return Reference to the description passed at creation.
        [[nodiscard]] virtual Link<const VirtualCameraDesc> getDesc() const noexcept = 0;

        /// @brief Returns a snapshot of the runtime counters.
        /// @return Current statistics.
        [[nodiscard]] virtual VirtualCameraStats getStats() const noexcept = 0;

        /// @brief Starts frame production.
        /// @return True when the camera started successfully.
        virtual bool Start() noexcept = 0;

        /// @brief Stops frame production.
        virtual void Stop() noexcept = 0;

        /// @brief Retries opening the consumer mapping.
        ///
        /// Call it periodically while the camera is alive; SubmitFrame also
        /// retries internally.
        virtual void Poll() noexcept = 0;

        /// @brief Publishes one frame to the consumer.
        ///
        /// The frame must match getDesc() in size and pixel format.
        ///
        /// @param frame Frame to publish; the data must remain valid during the call.
        /// @return False when the frame is invalid or no shared slot is available.
        virtual bool SubmitFrame(Link<const VideoFrame> frame) noexcept = 0;
    };

    /// @brief Creates virtual camera instances.
    class API IVirtualCameraFactory // NOLINT
    {
    public:
        /// @brief Virtual destructor for safe destruction through the interface.
        virtual ~IVirtualCameraFactory() = default;

        /// @brief Creates a camera with the given parameters.
        /// @param desc Creation parameters; copied by the factory.
        /// @return New camera, or null when creation fails.
        [[nodiscard]] virtual UniquePtr<IVirtualCamera> Create(
            Link<const VirtualCameraDesc> desc) = 0;
    };
}
