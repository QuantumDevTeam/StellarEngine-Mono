// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Memory/Allocator.h
/// @brief The allocators of the native core.
#pragma once

#include <cstddef>
#include <cstdint>

namespace Stellar::Native::Core::Memory
{
    /// @brief Gives native memory, takes it back and counts it: the StellarAllocator of Abi/Memory.h.
    ///
    /// Every engine has one, in its CoreContext; anything may make one of its own to count its memory apart (a
    /// profiler, a subsystem). It is counted by references: its maker holds one and each block not freed yet holds
    /// one, so a block freed after its engine is gone still finds its allocator, and what the engine leaked is in its
    /// stats when its context goes.
    ///
    /// Safe from any thread. Nothing here throws: out of memory is a null block. The header pulls in no threading
    /// header, so a C++/CLI translation unit may include it.
    class STELLAR_CORE_API Allocator final
    {
        int32_t _references = 1;
        int64_t _allocatedBytes = 0;
        int64_t _liveAllocations = 0;
        int64_t _totalAllocations = 0;
        int64_t _peakBytes = 0;

        Allocator() = default;
        ~Allocator() = default;

        void Count(int64_t bytes, int64_t blocks) noexcept;

    public:
        Allocator(const Allocator&) = delete;
        Allocator& operator=(const Allocator&) = delete;

        /// @brief Makes an allocator with one reference, the caller's.
        /// @return The allocator, or null when out of memory.
        [[nodiscard]] static Ptr<Allocator> Create() noexcept;

        /// @brief Adds a reference.
        void Acquire() noexcept;

        /// @brief Drops a reference: the last one destroys the allocator.
        void Release() noexcept;

        /// @brief Gives a block.
        /// @param size Its size in bytes.
        /// @param alignment Its alignment, a power of two.
        /// @return The block; null when out of memory, for a size of 0 or an alignment that is not a power of two.
        [[nodiscard]] Ptr<void> Allocate(size_t size, size_t alignment) noexcept;

        /// @brief Grows or shrinks a block, keeping its first bytes.
        /// @return The block, maybe moved; null when out of memory, and then the old block is untouched. A null
        /// block is allocated, a new size of 0 frees it.
        [[nodiscard]] Ptr<void> Reallocate(Ptr<void> memory, size_t oldSize, size_t newSize, size_t alignment) noexcept;

        /// @brief Frees a block this allocator gave, with the size it has. Null does nothing.
        void Free(Ptr<void> memory, size_t size) noexcept;

        /// @brief Reads what it holds now and held so far.
        [[nodiscard]] StellarMemoryStats GetStats() const noexcept;

        /// @brief Gets how many references it has.
        [[nodiscard]] int32_t References() const noexcept;

        /// @brief The allocator as the ABI names it.
        [[nodiscard]] Ptr<StellarAllocator> ToAbi() noexcept { return reinterpret_cast<StellarAllocator*>(this); }

        /// @brief The allocator an ABI pointer names.
        [[nodiscard]] static Ptr<Allocator> FromAbi(Ptr<StellarAllocator> allocator) noexcept
        {
            return reinterpret_cast<Allocator*>(allocator.get());
        }

        /// @brief The functions of the allocators, for code that does not link the native core.
        [[nodiscard]] static Link<const StellarMemoryApi> Api() noexcept;
    };

    /// @brief One reference to an allocator, dropped with this object.
    class AllocatorReference final
    {
        Ptr<Allocator> _allocator;

    public:
        /// @brief Takes over a reference the caller holds already (from Allocator::Create).
        explicit AllocatorReference(Ptr<Allocator> allocator) noexcept :
            _allocator(allocator) {}

        ~AllocatorReference()
        {
            if (_allocator)
                _allocator->Release();
        }

        AllocatorReference(const AllocatorReference&) = delete;
        AllocatorReference& operator=(const AllocatorReference&) = delete;

        [[nodiscard]] Link<Allocator> Get() const noexcept { return *_allocator; }
    };
}
