// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Failures/FailureLevel.h
/// @brief Severity of a native failure.
#pragma once

#include <cstdint>

namespace Stellar::Native::Core::Failures
{
    /// @brief Severity of a native failure. Each level includes the bits of the levels below it.
    enum class FailureLevel : uint8_t
    {
        NonCritical = 0b00001,
        Warning = 0b00011,
        Error = 0b00111,
        Critical = 0b01111,
    };
}
