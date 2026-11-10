// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Resource/ResourceCore.h
/// @brief CRTP base shared by backend resource implementations.
#pragma once

#include <string>

#include "Graphic/Contracts/Core/Enums.h"

#include "Interfaces.h"

namespace Stellar::Native::Kernel::Graphics::Resource::Core
{
    /// @brief CRTP base that implements the common IResource surface.
    ///
    /// Derived is expected to provide `void* NativeResource() const noexcept`.
    /// The base stores the recorded resource state and debug name, so backend
    /// implementations do not need to duplicate that bookkeeping.
    ///
    /// @tparam Derived Concrete resource type.
    /// @tparam TagT Handle tag identifying the resource kind.
    template <typename Derived, typename TagT>
    class API ResourceCore
            : public IResource
    {
    protected:
        ResourceState State = ResourceState::Common; ///< Last state declared by the caller.
        std::string DebugName{};                     ///< Debug name shown by validation/profiling tools.
        Ptr<Backend::IDevice> Device = nullptr;

        ResourceCore(Ptr<Backend::IDevice> device) noexcept
            : Device(std::move(device)) {}

    public:
        /// @brief Handle tag associated with this resource kind.
        using Tag = TagT;

        // IResource impl
        [[nodiscard]] Ptr<const Backend::IDevice> getDevice() const noexcept override { return Device; }

        /// @brief Forwards to Derived::NativeResource().
        /// @return Native resource handle.
        [[nodiscard]] void* getNativeHandle() const noexcept override
        {
            return static_cast<const Derived*>(this)->NativeResource();
        }

        /// @brief Returns the recorded resource state.
        /// @return Current resource state.
        [[nodiscard]] ResourceState getState() const noexcept override { return State; }
        /// @brief Updates the recorded resource state without emitting a barrier.
        /// @param s New state to record.
        void setState(ResourceState s) noexcept override { State = s; }

        /// @brief Returns the debug name.
        /// @return Debug name string view.
        [[nodiscard]] std::string_view getDebugName() const noexcept override { return DebugName; }
        /// @brief Replaces the debug name.
        /// @param name New name; moved into the resource.
        void setDebugName(std::string name) override { DebugName = std::move(name); }

        // CRTP access
        /// @brief Returns this object as const Derived.
        /// @return Reference to the derived object.
        [[nodiscard]] constexpr const Derived& Self() const noexcept
        {
            return static_cast<const Derived&>(*this);
        }

        /// @brief Returns this object as Derived.
        /// @return Reference to the derived object.
        [[nodiscard]] constexpr Derived& Self() noexcept
        {
            return static_cast<Derived&>(*this);
        }
    };
}
