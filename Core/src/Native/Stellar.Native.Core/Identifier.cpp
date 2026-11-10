#include "pch.h"
#include "Identifier.h"

#include <charconv>
#include <cstring>
#include <random>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <combaseapi.h>
#endif

#include "Failures/NativeException.h"

namespace Stellar::Native::Core
{
    namespace
    {
        // Text position of every byte. The first three groups of a GUID are little-endian in memory, so
        // the text and the memory order differ: this is what makes the form match System.Guid.
        constexpr std::array<std::size_t, 16> kBytePosition = {6, 4, 2, 0, 11, 9, 16, 14, 19, 21, 24, 26, 28, 30, 32, 34};
        constexpr std::size_t kTextLength = 36;
    }

    Identifier Identifier::Create()
    {
        Bytes bytes{};

#ifdef _WIN32
        GUID guid;

        if (FAILED(::CoCreateGuid(&guid)))
            throw Failures::NativeException("CoCreateGuid failed");

        std::memcpy(bytes.data(), &guid, bytes.size());
#else
        std::random_device device;

        for (auto& byte : bytes)
            byte = static_cast<uint8_t>(device());

        bytes[7] = static_cast<uint8_t>((bytes[7] & 0x0F) | 0x40); // version 4
        bytes[8] = static_cast<uint8_t>((bytes[8] & 0x3F) | 0x80); // RFC 4122 variant
#endif

        return Identifier(bytes);
    }

    std::optional<Identifier> Identifier::Parse(std::string_view text) noexcept
    {
        if (text.size() != kTextLength)
            return std::nullopt;

        for (const std::size_t dash : {8u, 13u, 18u, 23u})
            if (text[dash] != '-')
                return std::nullopt;

        Bytes bytes{};

        for (std::size_t i = 0; i < bytes.size(); ++i)
        {
            const Ptr<const char> begin = text.data() + kBytePosition[i];
            const auto [end, error] = std::from_chars(begin.get(), begin.get() + 2, bytes[i], 16);

            if (error != std::errc{} || end != begin.get() + 2)
                return std::nullopt;
        }

        return Identifier(bytes);
    }

    std::string Identifier::ToString() const
    {
        std::string text(kTextLength, '-');
        constexpr char digits[] = "0123456789abcdef";

        for (std::size_t i = 0; i < _data.size(); ++i)
        {
            text[kBytePosition[i]] = digits[_data[i] >> 4];
            text[kBytePosition[i] + 1] = digits[_data[i] & 0x0F];
        }

        return text;
    }

    uint64_t Identifier::GetHashCode() const noexcept
    {
        uint64_t low;
        uint64_t high;

        std::memcpy(&low, _data.data(), sizeof(low));
        std::memcpy(&high, _data.data() + sizeof(low), sizeof(high));

        return low ^ (high + 0x9E3779B97F4A7C15ull + (low << 6) + (low >> 2));
    }
}
