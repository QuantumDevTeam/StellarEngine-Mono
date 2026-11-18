// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/PInvoke/exports.h
/// @brief The flat exports of the native core, for P/Invoke.
///
/// This library only translates: every function calls a class of Stellar.Native.Core. Its one client is
/// Stellar.Native.Core.Wrapper.Managed, which turns these functions back into classes.
///
/// Conventions:
/// - every function returns a StellarResult and never throws, the details of a failure are in the last error of the
///   calling thread;
/// - a yes/no answer comes back through an `int32_t` out parameter, so that "no" is never mistaken for a failure;
/// - text is UTF-8. A function that fills a buffer always writes the required size to @c required and returns
///   STELLAR_RESULT_BUFFER_TOO_SMALL when it does not fit;
/// - pointer parameters are Ptr: pointer-sized and trivially copyable, so for the x64 calling convention they are
///   plain pointers;
/// - every function that reaches a registry takes the context it works in first (StellarCoreContext, one per
///   engine). Null is the default context of the process. A context is created and destroyed by the caller.
#pragma once

#ifdef STELLAR_PINVOKE_EXPORTS
#define STELLAR_PINVOKE_API STELLAR_EXTERN_C STELLAR_DLL_EXPORT
#else
#define STELLAR_PINVOKE_API STELLAR_EXTERN_C STELLAR_DLL_IMPORT
#endif

/// @brief Major version of these exports.
#define STELLAR_PINVOKE_ABI_MAJOR 3u

/// @brief Minor version of these exports.
#define STELLAR_PINVOKE_ABI_MINOR 1u

/// @brief The registries of one engine: an opaque Stellar::Native::Core::CoreContext.
using StellarCoreContext = struct StellarCoreContext;

// Library

STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_core_get_abi_version(Ptr<StellarContractVersion> version);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_core_get_last_error(Ptr<char> buffer, size_t capacity, Ptr<size_t> required);

// Contexts

/// @brief Creates a context, with empty registries.
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_core_context_create(Ptr<Ptr<StellarCoreContext>> context);

/// @brief Destroys a context and its registries, unloading its native modules. Null is refused.
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_core_context_destroy(Ptr<StellarCoreContext> context);

// Identifiers

STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_create(Ptr<StellarUid> identifier);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_parse(StellarStringView text, Ptr<StellarUid> identifier, Ptr<int32_t> parsed);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_format(Ptr<const StellarUid> identifier, Ptr<char> buffer, size_t capacity, Ptr<size_t> required);

// Memory (since 3.1)

/// @brief Gets the functions of the allocators (Abi/Memory.h). They are called directly, not through exports: a
/// block is given and freed without a P/Invoke stub.
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_memory_get_api(Ptr<Ptr<const StellarMemoryApi>> api);

/// @brief Gets the allocator of a context, without a reference: acquire it to keep it.
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_core_context_get_allocator(Ptr<StellarCoreContext> context, Ptr<Ptr<StellarAllocator>> allocator);

// Modules

STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_load(Ptr<StellarCoreContext> context, StellarStringView path, Ptr<StellarHandle> module);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_unload(Ptr<StellarCoreContext> context, StellarHandle module, Ptr<int32_t> unloaded);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_get_name(Ptr<StellarCoreContext> context, StellarHandle module, Ptr<char> buffer, size_t capacity, Ptr<size_t> required);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_count(Ptr<StellarCoreContext> context, Ptr<size_t> count);
