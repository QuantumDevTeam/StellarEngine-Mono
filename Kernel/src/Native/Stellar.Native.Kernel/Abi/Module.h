// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Abi/Module.h
/// @brief The contract of a native module: how it declares itself to the engine.
///
/// A native module is a shared library that exports one function, StellarModule_GetInfo. The loader calls it,
/// checks the contract version the module was built against and only then runs its code.
#ifndef STELLAR_KERNEL_ABI_MODULE_H
#define STELLAR_KERNEL_ABI_MODULE_H

#include "Abi/Abi.h"

/// @brief Major version of the module contract.
#define STELLAR_MODULE_CONTRACT_MAJOR 1u

/// @brief Minor version of the module contract.
#define STELLAR_MODULE_CONTRACT_MINOR 0u

/// @brief Name of the function a native module exports.
#define STELLAR_MODULE_ENTRY_NAME "StellarModule_GetInfo"

/// @brief Description of a native module.
using StellarModuleInfo = struct StellarModuleInfo
{
    /// @brief Size of this structure as the module knows it. New fields only ever go to the end.
    uint32_t structSize;

    /// @brief Version of the module contract the module was built against.
    StellarContractVersion contract;

    /// @brief Name of the module, with static lifetime.
    StellarStringView name;

    /// @brief Version of the module, with static lifetime.
    StellarStringView version;

    /// @brief Called once after a successful load. May be null.
    StellarResult (STELLAR_CALL*initialize)(void);

    /// @brief Called once before the unload. May be null.
    void (STELLAR_CALL*shutdown)(void);
};

/// @brief Signature of StellarModule_GetInfo.
using StellarModuleEntry = const StellarModuleInfo*(STELLAR_CALL*)(void);

#endif
