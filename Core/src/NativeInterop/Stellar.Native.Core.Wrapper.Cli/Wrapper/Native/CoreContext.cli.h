// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

#pragma once

#include "Wrapper/Interop.cli.h"

namespace Stellar::Core::Modules
{
    ref class ModuleRegistry;
}

namespace Stellar::Core::Native
{
    ref class NativeAllocator;

    using namespace System;
    using namespace System::Runtime::CompilerServices;

    /// <summary>
    /// The registries of the native core for one engine: identifiers, labels, containers, native modules.
    /// </summary>
    /// <remarks>
    /// <para>Every engine instance has its own context (<see cref="For"/>), so two engines in one process never see
    /// each other's identifiers or labels. <see cref="Current"/> is the context of the engine of the running code
    /// (<see cref="Stellar::Kernel::Lifecycle::EngineScope"/>), or <see cref="Default"/> outside any engine as <see cref="Stellar::Kernel::Lifecycle::EngineScope::Detached"/> allows.</para>
    /// <para>A context made with the constructor belongs to its maker, who disposes it.</para>
    /// </remarks>
    public ref class CoreContext sealed : IDisposable
    {
        static CoreContext^ _default = gcnew CoreContext(&nc::CoreContext::Default(), false);
        static ConditionalWeakTable<Stellar::Kernel::Lifecycle::IEngineInstance^, CoreContext^>^ _engines =
                gcnew ConditionalWeakTable<Stellar::Kernel::Lifecycle::IEngineInstance^, CoreContext^>();

        nc::CoreContext* _native;
        bool _owned;
        ::Stellar::Core::Modules::ModuleRegistry^ _modules;
        NativeAllocator^ _allocator;

        CoreContext(nc::CoreContext* native, bool owned);

        static CoreContext^ CreateFor(Stellar::Kernel::Lifecycle::IEngineInstance^ engine);

    public:
        /// <summary>Creates a context with empty registries.</summary>
        CoreContext();

        ~CoreContext();
        !CoreContext();

        /// <summary>Gets the default context of the process: for code outside any engine.</summary>
        static property CoreContext^ Default { CoreContext^ get() { return _default; } }

        /// <summary>Gets the context of the engine of the running code, or the default one.</summary>
        static property CoreContext^ Current { CoreContext^ get(); }

        /// <summary>Gets the context of an engine, created the first time.</summary>
        static CoreContext^ For(Stellar::Kernel::Lifecycle::IEngineInstance^ engine);

        /// <summary>Disposes the context of an engine, if it has one. <returns>Whether it had one.</returns></summary>
        static bool Release(Stellar::Kernel::Lifecycle::IEngineInstance^ engine);

        /// <summary>Gets whether this is the default context.</summary>
        property bool IsDefault { bool get() { return !_owned; } }

        /// <summary>Gets whether the context was disposed.</summary>
        property bool IsDisposed { bool get() { return _native == nullptr; } }

        /// <summary>Gets the registry of native modules.</summary>
        property ::Stellar::Core::Modules::ModuleRegistry^ Modules { ::Stellar::Core::Modules::ModuleRegistry^ get(); }

        /// <summary>
        /// Gets the allocator of the engine, made on first use. It outlives the context while blocks it gave are not
        /// freed.
        /// </summary>
        property NativeAllocator^ Allocator { NativeAllocator^ get(); }

    internal:
        nc::CoreContext& Native();
    };
}
