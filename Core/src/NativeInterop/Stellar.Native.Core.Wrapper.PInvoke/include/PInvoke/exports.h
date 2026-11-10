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
#define STELLAR_PINVOKE_ABI_MAJOR 2u

/// @brief Minor version of these exports.
#define STELLAR_PINVOKE_ABI_MINOR 0u

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
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_register(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> added);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_unregister(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> removed);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_contains(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> contained);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_count(Ptr<StellarCoreContext> context, Ptr<size_t> count);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_identifier_snapshot(Ptr<StellarCoreContext> context, Ptr<StellarUid> buffer, size_t capacity, Ptr<size_t> required);

// Labels

STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_label_register(Ptr<StellarCoreContext> context, StellarStringView name, Ptr<const StellarUid> identifier, Ptr<int32_t> added);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_label_find(Ptr<StellarCoreContext> context, StellarStringView name, Ptr<StellarUid> identifier, Ptr<int32_t> found);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_label_get_name(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<char> buffer, size_t capacity, Ptr<size_t> required);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_label_unregister(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> removed);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_label_contains(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> contained);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_label_count(Ptr<StellarCoreContext> context, Ptr<size_t> count);

// Containers

STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_create(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> created);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_destroy(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> destroyed);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_exists(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> exists);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_seal(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_is_sealed(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> sealed);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_add(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> item, Ptr<int32_t> added);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_remove(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> item, Ptr<int32_t> removed);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_contains(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> item, Ptr<int32_t> contained);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_count(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<size_t> count);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_items(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<StellarUid> buffer, size_t capacity, Ptr<size_t> required);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_clear(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> cleared);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_add_branch(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> branch, Ptr<int32_t> added);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_remove_branch(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> branch, Ptr<int32_t> removed);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_contains_branch(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> branch, Ptr<int32_t> contained);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_container_branches(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<StellarUid> buffer, size_t capacity, Ptr<size_t> required);

// Modules

STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_load(Ptr<StellarCoreContext> context, StellarStringView path, Ptr<StellarHandle> module);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_unload(Ptr<StellarCoreContext> context, StellarHandle module, Ptr<int32_t> unloaded);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_get_name(Ptr<StellarCoreContext> context, StellarHandle module, Ptr<char> buffer, size_t capacity, Ptr<size_t> required);
STELLAR_PINVOKE_API StellarResult STELLAR_CALL stellar_module_count(Ptr<StellarCoreContext> context, Ptr<size_t> count);
