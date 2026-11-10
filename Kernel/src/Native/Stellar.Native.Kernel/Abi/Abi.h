// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Abi/Abi.h
/// @brief The C ABI: the only types allowed to cross a module or a managed/native boundary.
///
/// Rules of the boundary (Docs/Architecture/NativeAbi.md):
/// - plain C structures with a fixed layout, identical on the C# side;
/// - handles instead of pointers;
/// - no exceptions: errors are StellarResult values, details are in the last error of the thread;
/// - every incompatible change of this file bumps the version in Data/version.props.
///
/// This header is valid C and includes nothing from the engine.
#ifndef STELLAR_KERNEL_ABI_H
#define STELLAR_KERNEL_ABI_H

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
#define STELLAR_EXTERN_C extern "C"
#else
#define STELLAR_EXTERN_C
#endif

#ifdef _WIN32
#define STELLAR_CALL __cdecl
#define STELLAR_DLL_EXPORT __declspec(dllexport)
#define STELLAR_DLL_IMPORT __declspec(dllimport)
#else
#define STELLAR_CALL
#define STELLAR_DLL_EXPORT __attribute__((visibility("default")))
#define STELLAR_DLL_IMPORT
#endif

/// @brief Version of a contract between two independently built parts of the engine.
///
/// A provider satisfies a consumer when the major versions are equal and the minor version of the provider is
/// not lower than the required one. Mirrors `Stellar.Kernel.Versioning.ContractVersion`.
using StellarContractVersion = struct StellarContractVersion
{
    uint32_t major;
    uint32_t minor;
};

/// @brief Value of an identifier: 16 bytes, the memory layout of a GUID and of `System.Guid`.
using StellarUid = struct StellarUid
{
    uint8_t bytes[16];
};

/// @brief Generational handle to an object owned by native code. A zero generation is an invalid handle.
///
/// Mirrors `Stellar.Kernel.Interop.NativeHandle`.
using StellarHandle = struct StellarHandle
{
    uint32_t index;
    uint32_t generation;
};

/// @brief A UTF-8 string that is not owned and not null-terminated.
using StellarStringView = struct StellarStringView
{
    const char* data;
    size_t size;
};

/// @brief Result of an ABI call. Mirrors `Stellar.Kernel.Interop.NativeResult`.
using StellarResult = int32_t;

enum
{
    STELLAR_RESULT_OK               = 0,
    STELLAR_RESULT_UNKNOWN          = 1,
    STELLAR_RESULT_INVALID_ARGUMENT = 2,
    STELLAR_RESULT_INVALID_HANDLE   = 3,
    STELLAR_RESULT_NOT_FOUND        = 4,
    STELLAR_RESULT_ALREADY_EXISTS   = 5,
    STELLAR_RESULT_BUFFER_TOO_SMALL = 6,
    STELLAR_RESULT_VERSION_MISMATCH = 7,
    STELLAR_RESULT_OUT_OF_MEMORY    = 8,
    STELLAR_RESULT_NATIVE_EXCEPTION = 9
};

#ifdef __cplusplus
static_assert(sizeof(StellarContractVersion) == 8, "StellarContractVersion layout is part of the ABI");
static_assert(sizeof(StellarUid) == 16, "StellarUid layout is part of the ABI");
static_assert(sizeof(StellarHandle) == 8, "StellarHandle layout is part of the ABI");
static_assert(sizeof(StellarResult) == 4, "StellarResult layout is part of the ABI");
#endif

#endif
