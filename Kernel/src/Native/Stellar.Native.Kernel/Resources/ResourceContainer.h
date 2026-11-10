// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Resources/ResourceContainer.h
/// @brief Binary layout of a compiled resource (.sres), see Docs/Formats/ResourceContainer.md.
///
/// The container is written by Stellar.Tools. Since all fields are little-endian and naturally aligned, a loaded
/// file can be read in place, with no parsing.
#pragma once

#include <cstdint>
#include <string_view>

/// @brief Contracts of the compiled resources.
namespace Stellar::Native::Kernel::Resources
{
    /// @brief Builds a four-character code, padding it with spaces.
    /// @param text At most four ASCII characters.
    /// @return The code: the first character is the lowest byte.
    [[nodiscard]] consteval uint32_t MakeFourCC(std::string_view text)
    {
        uint32_t value = 0;

        for (std::size_t i = 0; i < 4; ++i)
        {
            const char symbol = i < text.size() ? text[i] : ' ';
            value |= static_cast<uint32_t>(static_cast<uint8_t>(symbol)) << (i * 8);
        }

        return value;
    }

    /// @brief The magic number of a container.
    inline constexpr uint32_t kResourceMagic = MakeFourCC("SRES");

    /// @brief The container format version this header describes.
    inline constexpr uint16_t kResourceFormatVersion = 1;

    /// @brief The alignment of chunk data, in bytes.
    inline constexpr uint32_t kResourceChunkAlignment = 16;

    /// @brief The header at the very beginning of a container.
    struct ResourceHeader
    {
        uint32_t magic;
        uint16_t version;
        uint16_t flags;
        uint32_t type;
        uint32_t chunkCount;
    };

    /// @brief An entry of the chunk table, which directly follows the header.
    struct ResourceChunkEntry
    {
        uint32_t tag;
        uint32_t reserved;
        uint64_t offset;
        uint64_t size;
    };

    static_assert(sizeof(ResourceHeader) == 16, "ResourceHeader layout is part of the file format");
    static_assert(sizeof(ResourceChunkEntry) == 24, "ResourceChunkEntry layout is part of the file format");
}
