// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Native/NativeAllocator.cli.h
/// @brief The managed face of Stellar::Native::Core::Memory::Allocator.
#pragma once

#include "Wrapper/Interop.cli.h"

namespace Stellar::Core::Native
{
    using namespace System;

    /// <summary>
    /// An allocator of the native core: gives native memory, takes it back and counts it.
    /// </summary>
    /// <remarks>
    /// <para>Every engine has one (<see cref="CoreContext::Allocator"/>); anything may make one of its own to count its
    /// memory apart. The native allocator lives while it is referenced: by this object until it is collected, and by
    /// each block not freed yet, so a block freed after its engine is gone still finds it.</para>
    /// <para>The functions are direct C++ calls. Safe from any thread.</para>
    /// </remarks>
    // A finalizer and no destructor, on purpose: one allocator object is shared by everything that allocates from
    // it, so nobody may dispose it; it drops its reference when nothing uses it any more
#pragma warning(push)
#pragma warning(disable: 4461)
    public ref class NativeAllocator sealed
    {
        nc::Memory::Allocator* _native;

        static void CheckAlignment(UIntPtr alignment);

    internal:
        /// The allocator of a native one: adopts the reference of the caller, or takes one of its own.
        NativeAllocator(nc::Memory::Allocator* native, bool adopt);

    public:
        /// <summary>Makes an allocator of its own.</summary>
        NativeAllocator();

        !NativeAllocator();

        /// <summary>Gets the allocator of the default context: for code outside any engine.</summary>
        static property NativeAllocator^ Default { NativeAllocator^ get(); }

        /// <summary>Gets the native allocator, a <c>StellarAllocator*</c>: for a native list, or to give a native module.</summary>
        property IntPtr Handle { IntPtr get(); }

        /// <summary>Gets what it holds now and held so far.</summary>
        property Stellar::Kernel::Interop::NativeMemoryStats Stats { Stellar::Kernel::Interop::NativeMemoryStats get(); }

        /// <summary>Gives a block: 0 for a size of 0.</summary>
        /// <param name="size">Its size in bytes.</param>
        /// <param name="alignment">Its alignment, a power of two.</param>
        IntPtr Allocate(UIntPtr size, UIntPtr alignment);

        /// <summary>Grows or shrinks a block, keeping its first bytes: 0 allocates one, a new size of 0 frees it.</summary>
        IntPtr Reallocate(IntPtr memory, UIntPtr oldSize, UIntPtr newSize, UIntPtr alignment);

        /// <summary>Frees a block this allocator gave, with the size it has. 0 does nothing.</summary>
        void Free(IntPtr memory, UIntPtr size);

        virtual String^ ToString() override;
    };
#pragma warning(pop)
}
