// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/macro.h
/// @brief Export macros and compile-time interface checks.
#pragma once

/// @brief Marks a symbol as part of the kernel's public ABI.
///
/// The kernel is a static library of contracts, so it expands to nothing unless a module asks for an export:
/// `__declspec(dllexport)` with STELLAR_KERNEL_EXPORTS (or the older CPPDYNAMICLIBRARYTEMPLATE_EXPORTS),
/// nothing with STELLAR_KERNEL_STATIC or _LIB, and `__declspec(dllimport)` otherwise.
#if defined(STELLAR_KERNEL_EXPORTS) || defined(CPPDYNAMICLIBRARYTEMPLATE_EXPORTS)
#define API __declspec(dllexport)
#elif defined(STELLAR_KERNEL_STATIC) || defined(_LIB)
#define API
#else
#define API __declspec(dllimport)
#endif

/// @brief Lets a class inherit several empty mixins without growing.
#ifdef _MSC_VER
#define STELLAR_EMPTY_BASES __declspec(empty_bases)
#else
#define STELLAR_EMPTY_BASES
#endif

/// @brief Asserts that a class is a safely destructible abstract interface.
///
/// Place it in the implementation file of an interface to catch accidental
/// non-virtual destructors or concrete implementations.
///
/// @param cls Class to validate.
#define must_be_abstract(cls)\
static_assert(std::has_virtual_destructor_v<cls>, #cls " must be safely destructible through its interface");\
static_assert(std::is_abstract_v<cls>, #cls " must be an interface");

/// @brief Asserts that a class exposes a native handle.
///
/// The class must provide `getNativeHandle()` convertible to `void*`.
///
/// @param cls Class to validate.
#define must_provide_native_handle(cls)\
static_assert(Stellar::Native::Concepts::ProvidesNativeHandle<cls>, #cls " must provide native handle");
