// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
#pragma once
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <cstdint>
#include <type_traits>

/// @file Graphic/Contracts/Core/Enums/BufferFlags.h
/// @brief Capability and usage flags for GPU buffers.

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief Describes how a buffer will be bound and what views it needs.
    ///
    /// Flags are a declaration of intent: the backend uses them to choose the
    /// native resource description, bind flags and allowed views. Invalid
    /// combinations (for example both vertex and index usage on one buffer)
    /// are rejected during resource creation.
    enum class BufferFlags : uint32_t
    {
        None             = 0,        ///< No special capability; only explicit usage applies.
        VertexBuffer     = 1u << 0,  ///< Can be bound as a vertex buffer (VBO).
        IndexBuffer      = 1u << 1,  ///< Can be bound as an index buffer (IBO).
        ConstantBuffer   = 1u << 2,  ///< Can be bound as a constant buffer (CBV).
        StructuredBuffer = 1u << 3,  ///< Typed structured buffer with a non-zero element stride.
        IndirectArgs     = 1u << 4,  ///< Can store arguments for indirect draw/dispatch.
        ShaderResource   = 1u << 5,  ///< Can be read through a shader resource view (SRV).
        UnorderedAccess  = 1u << 6,  ///< Can be read/written through an unordered access view (UAV).
        RawBuffer        = 1u << 7,  ///< Untyped byte-address buffer (raw/structured without format).
    };
}

namespace Stellar::Native // opt-in flags // NOLINT
{
    /// @brief Enables bitwise flag operators for Graphics::BufferFlags.
    template <> struct IsFlagEnum<BufferFlags> : std::true_type {};
}
