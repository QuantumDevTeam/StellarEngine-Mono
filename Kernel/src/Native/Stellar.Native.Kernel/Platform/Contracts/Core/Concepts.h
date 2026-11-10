// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Platform/Contracts/Core/Concepts.h
/// @brief Concept used to validate platform backend descriptors.
#pragma once

#include <concepts>

#include "Platform/Types.h" // NOLINT

/// @brief Validation concepts for platform backends.
namespace Stellar::Native::Kernel::Platform::Concepts
{
    /// @brief A platform backend exposes the process-wide IPlatform service.
    ///
    /// Satisfied when `B::Default()` returns something convertible to
    /// `Ptr<IPlatform>`. The service owns its managers and lives until process
    /// exit, so the returned pointer is non-owning.
    template <typename B>
    concept Backend = requires
    {
        typename B::EventProc;
        typename B::EventTranslator;
        typename B::EventManager;

        typename B::Window;
        typename B::WindowClass;
        typename B::WindowManager;

        { B::Default() } -> std::convertible_to<Ptr<IPlatform>>;

        requires std::derived_from<typename B::EventProc, Events::IEventProc>;
        requires std::derived_from<typename B::EventTranslator, Events::IEventTranslator>;
        requires std::derived_from<typename B::EventManager, Events::IEventManager>;

        requires std::derived_from<typename B::Window, Window::IWindow>;
        requires std::derived_from<typename B::WindowClass, Window::IWindowClass>;
        requires std::derived_from<typename B::WindowManager, Window::IWindowManager>;
    };
}
