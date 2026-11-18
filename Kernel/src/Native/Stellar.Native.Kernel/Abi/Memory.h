// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Abi/Memory.h
/// @brief Native memory across the boundary: allocators and the layout of a native list.
///
/// Objects cross the boundary as handles; memory that both sides work on crosses as itself. A native list is a
/// header and a buffer in native memory: C# sees the buffer as a Span, C++ as a std::span, and a native module gets
/// the header without a copy. Memory is always freed by the allocator that gave it, through StellarMemoryApi, so a
/// module built against another C runtime never frees what it did not allocate.
#ifndef STELLAR_KERNEL_ABI_MEMORY_H
#define STELLAR_KERNEL_ABI_MEMORY_H

#include "Abi/Abi.h"

/// @brief Major version of the memory contract.
#define STELLAR_MEMORY_CONTRACT_MAJOR 1u

/// @brief Minor version of the memory contract.
#define STELLAR_MEMORY_CONTRACT_MINOR 0u

/// @brief An allocator of the native core: opaque, counted by references.
///
/// Every engine has one (its context's); anything may make one of its own to count its memory apart. An allocator
/// lives while it is referenced: by its maker, and by each block it gave that is not freed yet, so memory freed after
/// its engine is gone still finds its allocator.
using StellarAllocator = struct StellarAllocator;

/// @brief What an allocator holds now and held so far.
using StellarMemoryStats = struct StellarMemoryStats
{
    /// @brief Bytes in blocks not freed yet.
    uint64_t allocatedBytes;

    /// @brief Blocks not freed yet.
    uint64_t liveAllocations;

    /// @brief Blocks given since the allocator was made.
    uint64_t totalAllocations;

    /// @brief The most bytes held at once.
    uint64_t peakBytes;
};

/// @brief The functions of the allocators, for code that does not link the native core: the managed wrapper over
/// P/Invoke, native modules.
using StellarMemoryApi = struct StellarMemoryApi
{
    /// @brief Size of this structure as the core knows it. New fields only ever go to the end.
    uint32_t structSize;

    /// @brief Version of the memory contract.
    StellarContractVersion contract;

    /// @brief Makes an allocator with one reference, the caller's. Null when out of memory.
    StellarAllocator* (STELLAR_CALL*create)(void);

    /// @brief Adds a reference.
    void (STELLAR_CALL*acquire)(StellarAllocator* allocator);

    /// @brief Drops a reference: the last one destroys the allocator.
    void (STELLAR_CALL*release)(StellarAllocator* allocator);

    /// @brief Gives a block of @p size bytes aligned on @p alignment, a power of two. Null when out of memory, and for
    /// a size of 0.
    void* (STELLAR_CALL*allocate)(StellarAllocator* allocator, size_t size, size_t alignment);

    /// @brief Grows or shrinks a block, keeping its first bytes. Null when out of memory: the block is then untouched.
    void* (STELLAR_CALL*reallocate)(StellarAllocator* allocator, void* memory, size_t oldSize, size_t newSize,
                                    size_t alignment);

    /// @brief Frees a block the allocator gave, with the size it has. Null does nothing.
    void (STELLAR_CALL*free)(StellarAllocator* allocator, void* memory, size_t size);

    /// @brief Reads what the allocator holds.
    void (STELLAR_CALL*getStats)(const StellarAllocator* allocator, StellarMemoryStats* stats);
};

/// @brief Set in StellarNativeList::lock while a writer holds the list.
#define STELLAR_NATIVE_LIST_WRITER 0x40000000

/// @brief Set in StellarNativeList::lock while a writer waits: new readers wait behind it.
#define STELLAR_NATIVE_LIST_WAITING 0x20000000

/// @brief A native list: the header, in native memory, of a buffer of elements of one size.
///
/// The list that made the header owns it. Others read and change it through the lock, when the list is a concurrent
/// one: the number of readers in the low bits, STELLAR_NATIVE_LIST_WRITER for a writer, STELLAR_NATIVE_LIST_WAITING
/// for a writer that waits. Mirrors `Stellar.Core.Memory.NativeListHeader`.
using StellarNativeList = struct StellarNativeList
{
    /// @brief The elements; null while the capacity is 0.
    void* data;

    /// @brief How many elements there are.
    int32_t count;

    /// @brief How many elements the buffer holds.
    int32_t capacity;

    /// @brief Size of one element, in bytes: a reader checks it is the type it expects.
    int32_t elementSize;

    /// @brief The lock of a concurrent list; 0 for a list with one owner.
    int32_t lock;

    /// @brief Changes with every change of the elements' places: an enumeration checks it.
    uint32_t version;

    /// @brief Alignment of the buffer.
    uint32_t alignment;

    /// @brief The allocator of the header and of the buffer; the list holds a reference to it.
    StellarAllocator* allocator;
};

#ifdef __cplusplus
static_assert(sizeof(StellarMemoryStats) == 32, "StellarMemoryStats layout is part of the ABI");
static_assert(sizeof(void*) != 8 || sizeof(StellarNativeList) == 40, "StellarNativeList layout is part of the ABI");
#endif

#endif
