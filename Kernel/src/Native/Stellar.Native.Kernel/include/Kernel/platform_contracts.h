// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/platform_contracts.h
/// @brief Umbrella include for the standard platform contract surface.
///
/// The header pulls in core tags/enums/concepts/factory, event routing and
/// payloads, window descriptions/interfaces and the IPlatform backend
/// interface. Shell integration headers are opt-in and are not re-exported
/// here because they are platform-specific extensions.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

// Core
#include "../../Platform/Contracts/Core/Tags.h"
#include "../../Platform/Contracts/Core/Enums.h"
#include "../../Platform/Contracts/Core/Concepts.h"
#include "../../Platform/Contracts/Core/Factory.h"

// Events
#include "../../Platform/Contracts/Events/EventFlags.h"
#include "../../Platform/Contracts/Events/EventTypes.h"
#include "../../Platform/Contracts/Events/EventParams.h"
#include "../../Platform/Contracts/Events/Event.h"
#include "../../Platform/Contracts/Events/EventContext.h"
#include "../../Platform/Contracts/Events/EventPayloads.h"
#include "../../Platform/Contracts/Events/EventDelegate.h"
#include "../../Platform/Contracts/Events/Interfaces.h"

// Window
#include "../../Platform/Contracts/Window/Enums.h"
#include "../../Platform/Contracts/Window/Descriptions.h"
#include "../../Platform/Contracts/Window/Interfaces.h"

// Icon

// Cursor

// Display

// Mouse

// Backend
#include "../../Platform/Contracts/Backend/Interfaces.h"
