// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Core/Enums/Gamepad.h
/// @brief Gamepad buttons and analog axes.
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

namespace Stellar::Native::Kernel::Platform
{
    /// @brief Logical gamepad button identifier.
    ///
    /// The mapping is layout-independent: backends translate their native
    /// button codes to these values so game code can prompt for "A" or "LB"
    /// without knowing the controller family.
    enum class API GamepadButton : uint8_t
    {
        None = 0,   ///< No button / invalid value.
        A,          ///< South face button (Cross on PlayStation controllers).
        B,          ///< East face button (Circle).
        X,          ///< West face button (Square).
        Y,          ///< North face button (Triangle).
        LB,         ///< Left shoulder bumper.
        RB,         ///< Right shoulder bumper.
        Back,       ///< Back/View/Share button.
        Start,      ///< Start/Menu/Options button.
        Guide,      ///< Platform/Guide button.
        LeftStick,  ///< Left stick click (L3).
        RightStick, ///< Right stick click (R3).
        DPadUp,     ///< D-pad up.
        DPadDown,   ///< D-pad down.
        DPadLeft,   ///< D-pad left.
        DPadRight,  ///< D-pad right.
    };

    /// @brief Logical gamepad analog axis identifier.
    enum class API GamepadAxis : uint8_t
    {
        LeftX,       ///< Left stick horizontal axis, normally in [-1, 1].
        LeftY,       ///< Left stick vertical axis, normally in [-1, 1].
        RightX,      ///< Right stick horizontal axis, normally in [-1, 1].
        RightY,      ///< Right stick vertical axis, normally in [-1, 1].
        LeftTrigger, ///< Left trigger, normally in [0, 1].
        RightTrigger,///< Right trigger, normally in [0, 1].
    };
}
