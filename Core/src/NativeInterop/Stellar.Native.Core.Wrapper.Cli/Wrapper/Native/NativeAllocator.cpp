// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

#include "Wrapper/Native/NativeAllocator.cli.h"
#include "Wrapper/Native/CoreContext.cli.h"

using Stellar::Kernel::Interop::NativeMemoryStats;

namespace Stellar::Core::Native
{
    NativeAllocator::NativeAllocator(nc::Memory::Allocator* native, bool adopt) :
        _native(native)
    {
        if (_native == nullptr)
            throw gcnew OutOfMemoryException("The native core could not make an allocator");

        if (!adopt)
            _native->Acquire();
    }

    NativeAllocator::NativeAllocator() :
        NativeAllocator(nc::Memory::Allocator::Create().get(), true) {}

    NativeAllocator::!NativeAllocator()
    {
        if (_native != nullptr)
        {
            _native->Release();
            _native = nullptr;
        }
    }

    NativeAllocator^ NativeAllocator::Default::get()
    {
        return CoreContext::Default->Allocator;
    }

    IntPtr NativeAllocator::Handle::get()
    {
        return IntPtr(_native);
    }

    NativeMemoryStats NativeAllocator::Stats::get()
    {
        const StellarMemoryStats stats = _native->GetStats();
        return NativeMemoryStats(stats.allocatedBytes, stats.liveAllocations, stats.totalAllocations, stats.peakBytes);
    }

    void NativeAllocator::CheckAlignment(UIntPtr alignment)
    {
        const auto value = static_cast<size_t>(alignment.ToUInt64());
        if (value == 0 || (value & (value - 1)) != 0)
            throw gcnew ArgumentException(String::Format("An alignment is a power of two, not {0}", alignment), "alignment");
    }

    IntPtr NativeAllocator::Allocate(UIntPtr size, UIntPtr alignment)
    {
        CheckAlignment(alignment);
        const auto bytes = static_cast<size_t>(size.ToUInt64());
        void* memory = _native->Allocate(bytes, static_cast<size_t>(alignment.ToUInt64())).get();

        if (memory == nullptr && bytes != 0)
            throw gcnew OutOfMemoryException(String::Format("No native block of {0} bytes", size));

        return IntPtr(memory);
    }

    IntPtr NativeAllocator::Reallocate(IntPtr memory, UIntPtr oldSize, UIntPtr newSize, UIntPtr alignment)
    {
        CheckAlignment(alignment);
        const auto bytes = static_cast<size_t>(newSize.ToUInt64());
        void* moved = _native->Reallocate(memory.ToPointer(), static_cast<size_t>(oldSize.ToUInt64()), bytes,
                                          static_cast<size_t>(alignment.ToUInt64())).get();

        if (moved == nullptr && bytes != 0)
            throw gcnew OutOfMemoryException(String::Format("No native block of {0} bytes", newSize));

        return IntPtr(moved);
    }

    void NativeAllocator::Free(IntPtr memory, UIntPtr size)
    {
        _native->Free(memory.ToPointer(), static_cast<size_t>(size.ToUInt64()));
    }

    String^ NativeAllocator::ToString()
    {
        const StellarMemoryStats stats = _native->GetStats();
        return String::Format("Native allocator ({0} bytes in {1} blocks)", stats.allocatedBytes, stats.liveAllocations);
    }
}
