#include "include/Kernel/kernel.h"
#include "include/Core/core.h"
#include "include/Core/core_interop.h"
#include "include/PInvoke/exports.h"

#include <algorithm>
#include <cstring>
#include <vector>

using namespace Stellar::Native::Core;
using Failures::Guard;

namespace
{
    /// The context a call works in: the one given, or the default context of the process for null.
    [[nodiscard]] Link<CoreContext> ContextOf(Ptr<StellarCoreContext> context) noexcept
    {
        return context ? *reinterpret_cast<CoreContext*>(context.get()) : CoreContext::Default();
    }

    [[nodiscard]] std::string_view ToView(StellarStringView text) noexcept
    {
        return text.data == nullptr ? std::string_view{} : std::string_view(text.data, text.size);
    }

    [[nodiscard]] Identifier ToIdentifier(Ptr<const StellarUid> uid) noexcept
    {
        return Identifier(*uid);
    }

    [[nodiscard]] StellarResult Answer(Ptr<int32_t> out, bool value) noexcept
    {
        *out = value ? 1 : 0;
        return STELLAR_RESULT_OK;
    }

    /// Writes text with a terminator, following the buffer protocol of include/PInvoke/exports.h.
    [[nodiscard]] StellarResult CopyText(std::string_view text, Ptr<char> buffer, size_t capacity, Ptr<size_t> required) noexcept
    {
        if (!required)
            return STELLAR_RESULT_INVALID_ARGUMENT;

        *required = text.size();

        if (!buffer || capacity <= text.size())
            return STELLAR_RESULT_BUFFER_TOO_SMALL;

        std::memcpy(buffer.get(), text.data(), text.size());
        buffer.get()[text.size()] = '\0';

        return STELLAR_RESULT_OK;
    }

}

#define STELLAR_REQUIRE(...) \
    if (!(__VA_ARGS__))      \
        return STELLAR_RESULT_INVALID_ARGUMENT;

// Library

StellarResult STELLAR_CALL stellar_core_get_abi_version(Ptr<StellarContractVersion> version)
{
    STELLAR_REQUIRE(version)

    *version = {STELLAR_PINVOKE_ABI_MAJOR, STELLAR_PINVOKE_ABI_MINOR};
    return STELLAR_RESULT_OK;
}

StellarResult STELLAR_CALL stellar_core_get_last_error(Ptr<char> buffer, size_t capacity, Ptr<size_t> required)
{
    return CopyText(Failures::GetLastError(), buffer, capacity, required);
}

// Contexts

StellarResult STELLAR_CALL stellar_core_context_create(Ptr<Ptr<StellarCoreContext>> context)
{
    STELLAR_REQUIRE(context)

    return Guard([&]
    {
        *context = reinterpret_cast<StellarCoreContext*>(new CoreContext());
        return STELLAR_RESULT_OK;
    });
}

StellarResult STELLAR_CALL stellar_core_context_destroy(Ptr<StellarCoreContext> context)
{
    STELLAR_REQUIRE(context)

    return Guard([&]
    {
        delete reinterpret_cast<CoreContext*>(context.get());
        return STELLAR_RESULT_OK;
    });
}

// Identifiers

StellarResult STELLAR_CALL stellar_identifier_create(Ptr<StellarUid> identifier)
{
    STELLAR_REQUIRE(identifier)

    return Guard([&]
    {
        *identifier = Identifier::Create().ToAbi();
        return STELLAR_RESULT_OK;
    });
}

StellarResult STELLAR_CALL stellar_identifier_parse(StellarStringView text, Ptr<StellarUid> identifier, Ptr<int32_t> parsed)
{
    STELLAR_REQUIRE(identifier && parsed)

    const auto result = Identifier::Parse(ToView(text));
    if (result.has_value())
        *identifier = result->ToAbi();

    return Answer(parsed, result.has_value());
}

StellarResult STELLAR_CALL stellar_identifier_format(Ptr<const StellarUid> identifier, Ptr<char> buffer, size_t capacity, Ptr<size_t> required)
{
    STELLAR_REQUIRE(identifier)

    return Guard([&] { return CopyText(ToIdentifier(identifier).ToString(), buffer, capacity, required); });
}

// Memory

StellarResult STELLAR_CALL stellar_memory_get_api(Ptr<Ptr<const StellarMemoryApi>> api)
{
    STELLAR_REQUIRE(api)

    *api = &Memory::Allocator::Api();
    return STELLAR_RESULT_OK;
}

StellarResult STELLAR_CALL stellar_core_context_get_allocator(Ptr<StellarCoreContext> context, Ptr<Ptr<StellarAllocator>> allocator)
{
    STELLAR_REQUIRE(allocator)

    *allocator = ContextOf(context).Allocator().ToAbi();
    return STELLAR_RESULT_OK;
}

// Modules

StellarResult STELLAR_CALL stellar_module_load(Ptr<StellarCoreContext> context, StellarStringView path, Ptr<StellarHandle> module)
{
    STELLAR_REQUIRE(module)

    return Guard([&]
    {
        *module = ContextOf(context).Modules().Load(ToView(path));
        return STELLAR_RESULT_OK;
    });
}

StellarResult STELLAR_CALL stellar_module_unload(Ptr<StellarCoreContext> context, StellarHandle module, Ptr<int32_t> unloaded)
{
    STELLAR_REQUIRE(unloaded)

    return Guard([&] { return Answer(unloaded, ContextOf(context).Modules().Unload(module)); });
}

StellarResult STELLAR_CALL stellar_module_get_name(Ptr<StellarCoreContext> context, StellarHandle module, Ptr<char> buffer, size_t capacity, Ptr<size_t> required)
{
    return Guard([&]
    {
        const auto name = ContextOf(context).Modules().GetName(module);
        if (!name.has_value())
            return static_cast<StellarResult>(STELLAR_RESULT_INVALID_HANDLE);

        return CopyText(*name, buffer, capacity, required);
    });
}

StellarResult STELLAR_CALL stellar_module_count(Ptr<StellarCoreContext> context, Ptr<size_t> count)
{
    STELLAR_REQUIRE(count)

    *count = ContextOf(context).Modules().size();
    return STELLAR_RESULT_OK;
}
