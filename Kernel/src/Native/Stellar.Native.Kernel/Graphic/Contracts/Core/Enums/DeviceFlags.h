// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>
#include <type_traits>

/// @file Graphic/Contracts/Core/Enums/DeviceFlags.h
/// @brief Creation flags and adapter-selection hints for a GPU device.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Optional behavior and adapter hints passed to Backend::MakeDevice.
    ///
    /// Flags are hints unless the backend documents otherwise. A backend that
    /// cannot honor a hint falls back to its default device and reports the
    /// choice through AdapterInfo.
    enum class API DeviceFlags : uint32_t
    {
        None                   = 0,        ///< Default device with no debug or adapter preference.
        Debug                  = 1u << 0,  //< Enable backend validation and debug layers when available.
        UseAdapter             = 1u << 1,  ///< Honor the adapter selection implied by the other adapter flags.
        WarpAdapter            = 1u << 2,  ///< Prefer a software (WARP) adapter.
        HighPerformanceAdapter = 1u << 3,  ///< Prefer the discrete/high-performance adapter.
        MultiApi               = 1u << 4,  ///< Request a device that can interoperate with other graphics APIs.

        UseWarpAdapter            = UseAdapter | WarpAdapter,            ///< Shorthand for software adapter selection.
        UseHighPerformanceAdapter = UseAdapter | HighPerformanceAdapter, ///< Shorthand for high-performance selection.
    };
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    /// @brief Enables bitwise flag operators for Graphics::DeviceFlags.
    template <>
    struct IsFlagEnum<DeviceFlags> : std::true_type {};
}
