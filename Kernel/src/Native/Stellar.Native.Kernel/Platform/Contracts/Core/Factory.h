// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Core/Factory.h
/// @brief Backend-agnostic platform service factory.
#pragma once

#include "Platform/Types.h" // NOLINT
#include "Platform/Contracts/Core/Concepts.h"

namespace Stellar::Native::Kernel::Platform
{
    /// @brief Returns the process-wide platform service implemented by @p B.
    ///
    /// The backend type is not instantiated; `B::Default()` returns a
    /// non-owning pointer to a function-local singleton. The service keeps its
    /// window and event managers alive until process exit.
    ///
    /// @tparam B Platform backend descriptor (for example `Windows::PlatformBackend`).
    /// @return Non-owning pointer to the platform service; never null for a
    ///         conforming backend.
    template <Concepts::Backend B>
    [[nodiscard]] Ptr<IPlatform> MakePlatform() noexcept
    {
        return B::Default();
    }
}
