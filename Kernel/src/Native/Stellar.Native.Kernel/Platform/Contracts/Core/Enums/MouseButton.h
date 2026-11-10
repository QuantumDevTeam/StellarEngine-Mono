// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Core/Enums/MouseButton.h
/// @brief Mouse button identifiers.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform
{
    /// @brief Logical mouse button.
    enum class API MouseButton : uint8_t
    {
        None = 0, ///< No button / invalid value.
        Left,     ///< Primary (left) button.
        Right,    ///< Secondary (right) button.
        Middle,   ///< Middle button / wheel click.
        Extra1,   ///< First side button (usually "back").
        Extra2,   ///< Second side button (usually "forward").
    };
}
