// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Pipeline/Interfaces.h
/// @brief Shader compilation, compiled shaders and pipeline-state interfaces.
#pragma once

#include <string>

#include "Graphic/Types.h" // NOLINT
#include "Graphic/Contracts/Core/Enums.h"
#include "Graphic/Contracts/Core/Concepts.h"
#include "ShaderBlob.h"

namespace Stellar::Native::Kernel::Graphics::Pipeline
{
    /// @brief Compiles shader source text into backend bytecode.
    ///
    /// The compiler is backend-specific: the target profile and source
    /// language follow the backend's native toolchain. Implementations are
    /// expected to be stateless and safe to reuse for multiple compilations.
    class API IShaderCompiler // NOLINT
    {
    public:
        virtual ~IShaderCompiler() = default;

        /// @brief Compiles one shader entry point.
        /// @param source Shader source text.
        /// @param entryPoint Name of the function to compile.
        /// @param targetProfile Backend profile string (for example "vs_5_0" or "ps_6_0").
        /// @param debugName Debug name used by validation and profiling tools.
        /// @param outError Optional error sink; when non-null the compiler stores a
        ///        diagnostic message there on failure.
        /// @return Compiled bytecode, or an empty vector when compilation fails.
        virtual Ptr<ShaderBlob> Compile(
            std::string_view source,
            std::string_view entryPoint,
            std::string_view targetProfile,
            std::string_view debugName,
            Ptr<std::string> outError
        ) = 0;
    };

    must_be_abstract(IShaderCompiler)

    /// @brief A compiled shader stage and its backend bytecode.
    class API IShader // NOLINT
    {
    public:
        virtual ~IShader() = default;

        /// @brief Returns the device that created this shader.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;

        /// @brief Opaque native shader object (ID3D11VertexShader*, IDxcBlob, ...).
        /// @return Pointer to the native shader object.
        [[nodiscard]] virtual void* getNativeHandle() const noexcept = 0;
        /// @brief Pipeline stage this shader belongs to.
        /// @return Shader stage enum value.
        [[nodiscard]] virtual ShaderStage getStage() const noexcept = 0;
        [[nodiscard]] virtual Ptr<ShaderBlob> getBlob() const noexcept = 0;

        /// @brief Returns the debug name used by validation and profiling tools.
        /// @return Debug name string view.
        [[nodiscard]] virtual std::string_view getDebugName() const noexcept = 0;
        /// @brief Sets the debug name used by validation and profiling tools.
        /// @param name New name; copied into the context.
        virtual void setDebugName(std::string name) = 0;
    };

    must_be_abstract(IShader)
    must_provide_native_handle(IShader)
    static_assert(Concepts::HasDebugName<IShader>, "IShader must have a debug name");

    /// @brief Immutable graphics or compute pipeline state.
    ///
    /// The object is created by Backend::IDevice and can be bound to many
    /// command lists. It owns the native pipeline object, root signature and
    /// shader modules created from its description.
    class API IPipelineState // NOLINT
    {
    public:
        virtual ~IPipelineState() = default;

        /// @brief Returns the device that created this pipeline.
        /// @return Shared pointer to the creating device.
        [[nodiscard]] virtual Ptr<Backend::IDevice> getDevice() const noexcept = 0;

        /// @brief Opaque native pipeline object (ID3D12PipelineState*, VkPipeline, ...).
        /// @return Pointer to the native pipeline object.
        [[nodiscard]] virtual void* getNativeHandle() const noexcept = 0;
        /// @brief Opaque native root signature / pipeline layout, when the backend has one.
        /// @return Pointer to the native root signature, or null if the backend does not expose one.
        [[nodiscard]] virtual void* getNativeRootSignature() const noexcept = 0;

        /// @brief Returns the debug name used by validation and profiling tools.
        /// @return Debug name string view.
        [[nodiscard]] virtual std::string_view getDebugName() const noexcept = 0;
        /// @brief Sets the debug name used by validation and profiling tools.
        /// @param name New name; copied into the context.
        virtual void setDebugName(std::string name) = 0;
    };

    must_be_abstract(IPipelineState)
    must_provide_native_handle(IPipelineState)
    static_assert(Concepts::HasDebugName<IPipelineState>, "IPipelineState must have a debug name");
}
