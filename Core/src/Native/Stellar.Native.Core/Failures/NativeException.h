// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Failures/NativeException.h
/// @brief The exception of the native side. It never crosses the ABI, see Failures/Guard.h.
#pragma once

#include <format>
#include <stdexcept>
#include <string>

#include "Failures/FailureLevel.h"

namespace Stellar::Native::Core::Failures
{
    /// @brief The exception of the native side.
    class NativeException final : public std::runtime_error
    {
    public:
        /// @brief Creates an exception.
        /// @param message What happened. The text is copied.
        /// @param result The result code it maps to at the ABI boundary.
        /// @param level The severity.
        explicit NativeException(
            const std::string& message,
            StellarResult result = STELLAR_RESULT_UNKNOWN,
            FailureLevel level = FailureLevel::Critical) :
            std::runtime_error(message),
            _result(result),
            _level(level) {}

        /// @brief Gets the result code.
        [[nodiscard]] StellarResult GetResult() const noexcept { return _result; }

        /// @brief Gets the severity.
        [[nodiscard]] FailureLevel GetFailureLevel() const noexcept { return _level; }

        /// @brief Gets the text form.
        [[nodiscard]] std::string ToString() const
        {
            return std::format("NativeException#Message(\"{}\")", what());
        }

    private:
        StellarResult _result;
        FailureLevel _level;
    };
}
