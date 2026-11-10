// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Core/macro.h
/// @brief Export macros of the native core.
#pragma once

/// @brief Marks a symbol as part of the C++ surface of Stellar.Native.Core.
#ifdef STELLAR_CORE_EXPORTS
#define STELLAR_CORE_API STELLAR_DLL_EXPORT
#else
#define STELLAR_CORE_API STELLAR_DLL_IMPORT
#endif

/// @brief Marks a function as part of the C ABI of Stellar.Native.Core.
#define STELLAR_CORE_ABI STELLAR_EXTERN_C STELLAR_CORE_API
