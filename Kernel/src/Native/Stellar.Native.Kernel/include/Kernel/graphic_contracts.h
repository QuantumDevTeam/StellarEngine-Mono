// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file include/Kernel/graphic_contracts.h
/// @brief Umbrella include for the complete graphics contract surface.
///
/// The include order follows the dependency direction: core types first, then
/// resources, pipelines, commands, contexts and finally the device interface.
#pragma once
// ReSharper disable CppUnusedIncludeDirective

// Core
#include "../../Graphic/Contracts/Core/Enums.h"
#include "../../Graphic/Contracts/Core/Format.h"
#include "../../Graphic/Contracts/Core/Tags.h"
#include "../../Graphic/Contracts/Core/Handles.h"
#include "../../Graphic/Contracts/Core/Mixins.h"
#include "../../Graphic/Contracts/Core/Concepts.h"
#include "../../Graphic/Contracts/Core/Factory.h"

// Resource
#include "../../Graphic/Contracts/Resource/Descriptions.h"
#include "../../Graphic/Contracts/Resource/Interfaces.h"
#include "../../Graphic/Contracts/Resource/ResourceCore.h"

// Pipeline
#include "../../Graphic/Contracts/Pipeline/ShaderBlob.h"
#include "../../Graphic/Contracts/Pipeline/Descriptions.h"
#include "../../Graphic/Contracts/Pipeline/Interfaces.h"

// Command
#include "../../Graphic/Contracts/Command/Interfaces.h"

// Context
#include "../../Graphic/Contracts/Context/Descriptions.h"
#include "../../Graphic/Contracts/Context/Interfaces.h"

// Backend
#include "../../Graphic/Contracts/Backend/Interfaces.h"
