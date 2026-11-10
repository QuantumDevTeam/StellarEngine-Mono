// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Core/Factory.h
/// @brief Backend-agnostic GPU device factory.
#pragma once

#include "Graphic/Types.h" // NOLINT
#include "Graphic/Contracts/Core/Enums.h"
#include "Graphic/Contracts/Core/Concepts.h"

namespace Stellar::Native::Kernel::Graphics::Backend
{
    /// @brief Creates the GPU device implemented by backend @p B.
    ///
    /// The backend type only needs to satisfy Concepts::Backend; it is not
    /// instantiated, and `B::Create` performs the actual device creation.
    /// Ownership of the returned object is shared with the caller and with any
    /// resources created from it.
    ///
    /// @tparam B Graphics backend descriptor (for example `DX11::Backend`).
    /// @param flags Optional debug and adapter-selection hints.
    /// @return The created device, or null when the backend cannot create one.
    ///
    /// @note A failed creation is reported by returning null; the backend also
    /// logs the reason through its diagnostics channel.
    template <Concepts::Backend B>
    [[nodiscard]] Ptr<IDevice> MakeDevice(DeviceFlags flags = DeviceFlags::None)
    {
        return B::Create(flags);
    }
}
