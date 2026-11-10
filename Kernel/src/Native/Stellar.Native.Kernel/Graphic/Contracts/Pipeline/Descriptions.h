// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Graphic/Contracts/Pipeline/Descriptions.h
/// @brief Immutable pipeline-state descriptions shared by all backends.
#pragma once
// ReSharper disable CppClangTidyClangDiagnosticPadded
// ReSharper disable CppClangTidyPerformanceEnumSize

#include <array>
#include <string>
#include <vector>

#include "Graphic/Contracts/Core/Enums.h"
#include "Graphic/Contracts/Core/Format.h"

#include "ShaderBlob.h"

namespace Stellar::Native::Kernel::Graphics
{
    /// @brief One vertex input element in a pipeline input layout.
    struct API InputElement
    {
        std::string semanticName;        ///< HLSL semantic name, for example "POSITION".
        uint32_t semanticIndex = 0;      ///< Semantic index for repeated names (TEXCOORD0, TEXCOORD1, ...).
        Format format = Format::Unknown; ///< Component format of the element.
        uint32_t inputSlot = 0;          ///< Vertex-buffer input slot that supplies the element.
        uint32_t alignedByteOffset = 0;  ///< Byte offset inside the vertex, or AppendOffset to append.
        bool perInstance = false;        ///< Advance the element once per instance instead of per vertex.
        uint32_t instanceStepRate = 0;   ///< Instances between element advances when perInstance is true.

        /// @brief Sentinel for alignedByteOffset: append after the previous element.
        static constexpr uint32_t AppendOffset = 0xFFFFFFFF;
    };

    /// @brief Rasterizer state.
    struct API RasterizerDesc
    {
        FillMode fillMode = FillMode::Solid;    ///< Polygon fill mode.
        CullMode cullMode = CullMode::Back;     ///< Which triangle facing is culled.
        FrontFace frontFace = FrontFace::Clockwise;   ///< Winding order that defines a front face.
        int32_t depthBias = 0;                  ///< Constant depth offset added to fragments.
        float depthBiasClamp = 0.0f;            ///< Maximum absolute depth bias; 0 disables clamping.
        float slopeScaledDepthBias = 0.0f;      ///< Depth bias scaled by the polygon slope.
        bool depthClipEnable = true;            ///< Clip fragments outside the depth range.
        bool scissorEnable = false;             ///< Enable scissor-rectangle clipping.
        bool multisampleEnable = false;         ///< Enable multisample rasterization.
        bool antialiasedLineEnable = false;     ///< Enable line antialiasing where supported.
        uint32_t forcedSampleCount = 0;         ///< Override the sample count; 0 keeps the target count.
        ConservativeRaster conservativeRaster = ConservativeRaster::Off; ///< Conservative rasterization mode.
    };

    /// @brief Blend state for one render target.
    struct API BlendTargetDesc
    {
        bool blendEnable = false;                   ///< Enable arithmetic blending for this target.
        BlendFactor srcBlend = BlendFactor::One;    ///< Multiplier for the source color.
        BlendFactor destBlend = BlendFactor::Zero;  ///< Multiplier for the destination color.
        BlendOp blendOp = BlendOp::Add;             ///< Color combination operation.
        BlendFactor srcBlendAlpha = BlendFactor::One;    ///< Multiplier for the source alpha.
        BlendFactor destBlendAlpha = BlendFactor::Zero;  ///< Multiplier for the destination alpha.
        BlendOp blendOpAlpha = BlendOp::Add;         ///< Alpha combination operation.
        bool logicOpEnable = false;                  ///< Use a bitwise logic operation instead of blending.
        LogicOp logicOp = LogicOp::Noop;             ///< Bitwise operation when logicOpEnable is true.
        ColorWriteMask writeMask = ColorWriteMask::All;  ///< Channels written by this target.
    };

    /// @brief Blend state for all render targets in a pipeline.
    struct API BlendDesc
    {
        bool alphaToCoverageEnable = false;   ///< Convert alpha to a temporary coverage mask.
        bool independentBlendEnable = false;  ///< Allow each target to use its own blend settings.
        BlendTargetDesc targets[8]{};   ///< Per-target blend state; only targetCount entries are used.
        uint32_t targetCount = 1;       ///< Number of active render target blend entries.

        /// @brief Creates an opaque blend state: blending disabled, all channels written.
        /// @return Blend state with blending disabled.
        [[nodiscard]] static BlendDesc Opaque() noexcept
        {
            BlendDesc d{};
            d.targets[0].blendEnable = false;
            d.targets[0].writeMask = ColorWriteMask::All;
            return d;
        }

        /// @brief Creates standard source-alpha blending.
        /// @details `src * srcAlpha + dst * (1 - srcAlpha)` for color and
        ///          `src + dst * (1 - srcAlpha)` for alpha.
        /// @return Blend state configured for source-alpha blending.
        [[nodiscard]] static BlendDesc AlphaBlend() noexcept
        {
            BlendDesc d{};
            d.targets[0].blendEnable = true;
            d.targets[0].srcBlend = BlendFactor::SrcAlpha;
            d.targets[0].destBlend = BlendFactor::InvSrcAlpha;
            d.targets[0].blendOp = BlendOp::Add;
            d.targets[0].srcBlendAlpha = BlendFactor::One;
            d.targets[0].destBlendAlpha = BlendFactor::InvSrcAlpha;
            d.targets[0].blendOpAlpha = BlendOp::Add;
            d.targets[0].writeMask = ColorWriteMask::All;
            return d;
        }

        /// @brief Creates additive blending: `src * 1 + dst * 1`.
        /// @return Blend state configured for additive blending.
        [[nodiscard]] static BlendDesc Additive() noexcept
        {
            BlendDesc d{};
            d.targets[0].blendEnable = true;
            d.targets[0].srcBlend = BlendFactor::One;
            d.targets[0].destBlend = BlendFactor::One;
            d.targets[0].blendOp = BlendOp::Add;
            d.targets[0].writeMask = ColorWriteMask::All;
            return d;
        }
    };

    /// @brief Stencil operations for one triangle facing.
    struct API StencilOpDesc
    {
        StencilOp failOp = StencilOp::Keep;      ///< Applied when the stencil test fails.
        StencilOp depthFailOp = StencilOp::Keep; ///< Applied when stencil passes but depth fails.
        StencilOp passOp = StencilOp::Keep;      ///< Applied when both tests pass.
        CompareOp compareOp = CompareOp::Always; ///< Comparison against the stencil reference.
    };

    /// @brief Depth and stencil test state.
    struct API DepthStencilDesc
    {
        bool depthEnable = true;            ///< Enable depth testing.
        bool depthWrite = true;             ///< Write passing fragments to the depth buffer.
        CompareOp depthFunc = CompareOp::LessEqual; ///< Depth comparison function.
        bool stencilEnable = false;         ///< Enable stencil testing.
        uint8_t stencilReadMask = 0xFF;     ///< Mask applied to stencil values before comparison.
        uint8_t stencilWriteMask = 0xFF;    ///< Mask applied to stencil values before writing.
        StencilOpDesc frontFace{};          ///< Stencil state for front-facing triangles.
        StencilOpDesc backFace{};           ///< Stencil state for back-facing triangles.

        /// @brief Creates the default depth state: depth test LessEqual, writes enabled.
        /// @return Default depth/stencil state.
        [[nodiscard]] static DepthStencilDesc Default() noexcept
        {
            return DepthStencilDesc{};
        }

        /// @brief Creates a state with depth and stencil testing disabled.
        /// @return Depth/stencil state with all tests disabled.
        [[nodiscard]] static DepthStencilDesc NoDepth() noexcept
        {
            DepthStencilDesc d{};
            d.depthEnable = false;
            d.depthWrite = false;
            d.stencilEnable = false;
            return d;
        }
    };

    /// @brief Sampler state description, including optional static-sampler binding.
    struct API SamplerDesc
    {
        Filter filter = Filter::MinMagMipLinear;      ///< Minification/magnification/mip filtering.
        TextureAddressMode addressU = TextureAddressMode::Wrap; ///< U coordinate addressing.
        TextureAddressMode addressV = TextureAddressMode::Wrap; ///< V coordinate addressing.
        TextureAddressMode addressW = TextureAddressMode::Wrap; ///< W coordinate addressing.
        float mipLODBias = 0.0f;                      ///< Bias added to the computed mip level.
        uint32_t maxAnisotropy = 16;                  ///< Maximum anisotropy for anisotropic filters.
        CompareOp comparisonFunc = CompareOp::Never;  ///< Comparison used by comparison filters.
        BorderColor borderColor = BorderColor::TransparentBlack; ///< Color used by Border addressing.
        float minLOD = 0.0f;                          ///< Lowest mip level that can be sampled.
        float maxLOD = 3.402823466e+38f;              ///< Highest mip level that can be sampled.
        uint32_t shaderRegister = 0;                  ///< Register slot for a static sampler.
        uint32_t registerSpace = 0;                   ///< Register space for a static sampler.
        ShaderStage visibility = ShaderStage::All;    ///< Stages that can use the static sampler.
    };

    /// @brief Push-constant range visible to one or more shader stages.
    struct API PushConstantRange
    {
        ShaderStage stages = ShaderStage::All; ///< Stages that can read the range.
        uint32_t offset = 0;      ///< Byte offset in the push-constant block.
        uint32_t size = 0;        ///< Size of the range in bytes.
    };

    /// @brief One entry in a pipeline binding layout.
    struct API BindingEntry
    {
        BindingType type = BindingType::ConstantBuffer; ///< Kind of view bound at the slot.
        ShaderStage stage = ShaderStage::All;   ///< Stages that can access the binding.
        uint32_t slot = 0;                      ///< Register/descriptor slot.
    };

    /// @brief Declares the resource bindings expected by a pipeline.
    struct API BindingLayout
    {
        std::vector<BindingEntry> entries{};           ///< Resource bindings in declaration order.
        std::vector<PushConstantRange> pushConstants{};///< Push-constant ranges used by the shaders.
        std::vector<SamplerDesc> staticSamplers{};     ///< Samplers embedded in the pipeline layout.
    };

    /// @brief Complete description of a graphics pipeline state.
    struct API PipelineDesc
    {
        Ptr<ShaderBlob> vertexShader = nullptr;  ///< Vertex shader bytecode.
        Ptr<ShaderBlob> pixelShader = nullptr;   ///< Pixel (fragment) shader bytecode.
        Ptr<ShaderBlob> geometryShader = nullptr;///< Optional geometry shader bytecode.
        Ptr<ShaderBlob> hullShader = nullptr;    ///< Optional hull (tessellation control) shader bytecode.
        Ptr<ShaderBlob> domainShader = nullptr;  ///< Optional domain (tessellation evaluation) shader bytecode.

        std::vector<InputElement> inputLayout{}; ///< Vertex input elements consumed by the vertex shader.

        Topology topology = Topology::TriangleList;    ///< Input-assembly primitive topology.
        TopologyType topologyType = TopologyType::Triangle; ///< Primitive family used for validation.

        RasterizerDesc rasterizer = RasterizerDesc{};          ///< Rasterizer state.
        BlendDesc blend = BlendDesc::Opaque();                 ///< Blend state.
        DepthStencilDesc depthStencil = DepthStencilDesc::Default(); ///< Depth/stencil state.

        std::array<Format, 8> rtvFormats{}; ///< Color render-target formats; first rtvCount entries are used.
        uint32_t rtvCount = 0;              ///< Number of active render targets.
        Format dsvFormat = Format::Unknown; ///< Depth/stencil format, or Unknown when depth is unused.

        uint32_t sampleCount = 1;          ///< Multisample count; must match the render targets.
        uint32_t sampleMask = 0xFFFFFFFFu; ///< Sample coverage mask.

        BindingLayout bindings{}; ///< Resource bindings and static samplers.

        std::string debugName{};  ///< Name shown by validation and profiling tools.
    };

    /// @brief Complete description of a compute pipeline state.
    struct API ComputePipelineDesc
    {
        Ptr<ShaderBlob> computeShader{}; ///< Compute shader bytecode.
        BindingLayout bindings{};   ///< Resource bindings and static samplers.

        std::string debugName{}; ///< Name shown by validation and profiling tools.
    };
}
