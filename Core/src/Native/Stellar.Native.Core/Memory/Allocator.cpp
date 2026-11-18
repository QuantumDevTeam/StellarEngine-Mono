// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

#include "pch.h"
#include "Memory/Allocator.h"

#include <algorithm>
#include <atomic>
#include <bit>
#include <cstdlib>
#include <cstring>
#include <new>

namespace Stellar::Native::Core::Memory
{
    namespace
    {
        // The counters are plain integers in the header, so that C++/CLI may include it: atomic here
        template <typename T>
        [[nodiscard]] std::atomic_ref<T> Atomic(const T& value) noexcept
        {
            return std::atomic_ref<T>(const_cast<T&>(value));
        }

        [[nodiscard]] bool IsValidAlignment(size_t alignment) noexcept
        {
            return std::has_single_bit(alignment);
        }

        [[nodiscard]] void* RawAllocate(size_t size, size_t alignment) noexcept
        {
#ifdef _WIN32
            return _aligned_malloc(size, alignment);
#else
            alignment = std::max(alignment, sizeof(void*));
            return std::aligned_alloc(alignment, (size + alignment - 1) / alignment * alignment);
#endif
        }

        [[nodiscard]] void* RawReallocate(void* memory, size_t oldSize, size_t newSize, size_t alignment) noexcept
        {
#ifdef _WIN32
            (void)oldSize;
            return _aligned_realloc(memory, newSize, alignment);
#else
            void* moved = RawAllocate(newSize, alignment);
            if (moved)
            {
                std::memcpy(moved, memory, std::min(oldSize, newSize));
                std::free(memory);
            }

            return moved;
#endif
        }

        void RawFree(void* memory) noexcept
        {
#ifdef _WIN32
            _aligned_free(memory);
#else
            std::free(memory);
#endif
        }

        // The ABI functions: thin, over the class
        StellarAllocator* STELLAR_CALL AbiCreate() noexcept
        {
            const auto allocator = Allocator::Create();
            return allocator ? allocator->ToAbi().get() : nullptr;
        }

        void STELLAR_CALL AbiAcquire(StellarAllocator* allocator) noexcept
        {
            if (allocator)
                Allocator::FromAbi(allocator)->Acquire();
        }

        void STELLAR_CALL AbiRelease(StellarAllocator* allocator) noexcept
        {
            if (allocator)
                Allocator::FromAbi(allocator)->Release();
        }

        void* STELLAR_CALL AbiAllocate(StellarAllocator* allocator, size_t size, size_t alignment) noexcept
        {
            return allocator ? Allocator::FromAbi(allocator)->Allocate(size, alignment).get() : nullptr;
        }

        void* STELLAR_CALL AbiReallocate(StellarAllocator* allocator, void* memory, size_t oldSize, size_t newSize,
                                         size_t alignment) noexcept
        {
            return allocator ? Allocator::FromAbi(allocator)->Reallocate(memory, oldSize, newSize, alignment).get() : nullptr;
        }

        void STELLAR_CALL AbiFree(StellarAllocator* allocator, void* memory, size_t size) noexcept
        {
            if (allocator)
                Allocator::FromAbi(allocator)->Free(memory, size);
        }

        void STELLAR_CALL AbiGetStats(const StellarAllocator* allocator, StellarMemoryStats* stats) noexcept
        {
            if (allocator && stats)
                *stats = reinterpret_cast<const Allocator*>(allocator)->GetStats();
        }
    }

    Ptr<Allocator> Allocator::Create() noexcept
    {
        return new (std::nothrow) Allocator();
    }

    void Allocator::Acquire() noexcept
    {
        Atomic(_references).fetch_add(1, std::memory_order_relaxed);
    }

    void Allocator::Release() noexcept
    {
        if (Atomic(_references).fetch_sub(1, std::memory_order_acq_rel) == 1)
            delete this;
    }

    Ptr<void> Allocator::Allocate(size_t size, size_t alignment) noexcept
    {
        if (size == 0 || !IsValidAlignment(alignment))
            return nullptr;

        void* memory = RawAllocate(size, alignment);
        if (!memory)
            return nullptr;

        Acquire();
        Count(static_cast<int64_t>(size), 1);
        return memory;
    }

    Ptr<void> Allocator::Reallocate(Ptr<void> memory, size_t oldSize, size_t newSize, size_t alignment) noexcept
    {
        if (!memory)
            return Allocate(newSize, alignment);

        if (newSize == 0)
        {
            Free(memory, oldSize);
            return nullptr;
        }

        if (!IsValidAlignment(alignment))
            return nullptr;

        void* moved = RawReallocate(memory.get(), oldSize, newSize, alignment);
        if (!moved)
            return nullptr;

        Count(static_cast<int64_t>(newSize) - static_cast<int64_t>(oldSize), 0);
        return moved;
    }

    void Allocator::Free(Ptr<void> memory, size_t size) noexcept
    {
        if (!memory)
            return;

        RawFree(memory.get());
        Count(-static_cast<int64_t>(size), -1);
        Release();
    }

    StellarMemoryStats Allocator::GetStats() const noexcept
    {
        return StellarMemoryStats{
            static_cast<uint64_t>(Atomic(_allocatedBytes).load(std::memory_order_relaxed)),
            static_cast<uint64_t>(Atomic(_liveAllocations).load(std::memory_order_relaxed)),
            static_cast<uint64_t>(Atomic(_totalAllocations).load(std::memory_order_relaxed)),
            static_cast<uint64_t>(Atomic(_peakBytes).load(std::memory_order_relaxed)),
        };
    }

    int32_t Allocator::References() const noexcept
    {
        return Atomic(_references).load(std::memory_order_relaxed);
    }

    void Allocator::Count(int64_t bytes, int64_t blocks) noexcept
    {
        const int64_t now = Atomic(_allocatedBytes).fetch_add(bytes, std::memory_order_relaxed) + bytes;

        if (blocks != 0)
            Atomic(_liveAllocations).fetch_add(blocks, std::memory_order_relaxed);
        if (blocks > 0)
            Atomic(_totalAllocations).fetch_add(blocks, std::memory_order_relaxed);

        auto peak = Atomic(_peakBytes);
        int64_t seen = peak.load(std::memory_order_relaxed);
        while (now > seen && !peak.compare_exchange_weak(seen, now, std::memory_order_relaxed))
        {
        }
    }

    Link<const StellarMemoryApi> Allocator::Api() noexcept
    {
        static constexpr StellarMemoryApi api{
            sizeof(StellarMemoryApi),
            {STELLAR_MEMORY_CONTRACT_MAJOR, STELLAR_MEMORY_CONTRACT_MINOR},
            &AbiCreate,
            &AbiAcquire,
            &AbiRelease,
            &AbiAllocate,
            &AbiReallocate,
            &AbiFree,
            &AbiGetStats,
        };

        return api;
    }
}
