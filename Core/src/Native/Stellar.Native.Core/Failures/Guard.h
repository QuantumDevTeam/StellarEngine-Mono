// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Failures/Guard.h
/// @brief A boundary of the core: exceptions stop here and turn into result codes.
#pragma once

#include <exception>
#include <new>
#include <string>
#include <string_view>

#include "Failures/NativeException.h"

namespace Stellar::Native::Core::Failures
{
    /// @brief Stores the message of a failure for the calling thread.
    STELLAR_CORE_API void SetLastError(std::string_view message);

    /// @brief Gets the message of the last failure of the calling thread.
    [[nodiscard]] STELLAR_CORE_API Link<const std::string> GetLastError() noexcept;

    /// @brief Runs a function at a boundary that exceptions must not cross.
    /// @param function Returns a StellarResult and may throw.
    /// @return Its result, or the code of the exception it threw.
    template <typename F>
    [[nodiscard]] StellarResult Guard(F&& function) noexcept
    {
        try
        {
            return function();
        }
        catch (Link<const NativeException> exception)
        {
            SetLastError(exception.what());
            return exception.GetResult();
        }
        catch (Link<const std::bad_alloc>)
        {
            return STELLAR_RESULT_OUT_OF_MEMORY;
        }
        catch (Link<const std::exception> exception)
        {
            SetLastError(exception.what());
            return STELLAR_RESULT_NATIVE_EXCEPTION;
        }
        catch (...)
        {
            SetLastError("Unknown native exception");
            return STELLAR_RESULT_NATIVE_EXCEPTION;
        }
    }
}
