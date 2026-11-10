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

    /// Writes identifiers, following the same buffer protocol, counted in identifiers.
    [[nodiscard]] StellarResult CopyIdentifiers(
        Link<const std::vector<Identifier>> identifiers, Ptr<StellarUid> buffer, size_t capacity, Ptr<size_t> required) noexcept
    {
        if (!required)
            return STELLAR_RESULT_INVALID_ARGUMENT;

        *required = identifiers.size();

        if (capacity < identifiers.size() || (!buffer && !identifiers.empty()))
            return STELLAR_RESULT_BUFFER_TOO_SMALL;

        std::transform(identifiers.begin(), identifiers.end(), buffer.get(),
                       [](Link<const Identifier> id) { return id.ToAbi(); });

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

StellarResult STELLAR_CALL stellar_identifier_register(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> added)
{
    STELLAR_REQUIRE(identifier && added)

    return Guard([&] { return Answer(added, ContextOf(context).Identifiers().Register(ToIdentifier(identifier))); });
}

StellarResult STELLAR_CALL stellar_identifier_unregister(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> removed)
{
    STELLAR_REQUIRE(identifier && removed)

    return Guard([&] { return Answer(removed, ContextOf(context).Identifiers().Unregister(ToIdentifier(identifier))); });
}

StellarResult STELLAR_CALL stellar_identifier_contains(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> contained)
{
    STELLAR_REQUIRE(identifier && contained)

    return Guard([&] { return Answer(contained, ContextOf(context).Identifiers().Contains(ToIdentifier(identifier))); });
}

StellarResult STELLAR_CALL stellar_identifier_count(Ptr<StellarCoreContext> context, Ptr<size_t> count)
{
    STELLAR_REQUIRE(count)

    *count = ContextOf(context).Identifiers().size();
    return STELLAR_RESULT_OK;
}

StellarResult STELLAR_CALL stellar_identifier_snapshot(Ptr<StellarCoreContext> context, Ptr<StellarUid> buffer, size_t capacity, Ptr<size_t> required)
{
    return Guard([&] { return CopyIdentifiers(ContextOf(context).Identifiers().Snapshot(), buffer, capacity, required); });
}

// Labels

StellarResult STELLAR_CALL stellar_label_register(Ptr<StellarCoreContext> context, StellarStringView name, Ptr<const StellarUid> identifier, Ptr<int32_t> added)
{
    STELLAR_REQUIRE(identifier && added)

    return Guard([&]
    {
        return Answer(added, ContextOf(context).Labels().Register(ToView(name), ToIdentifier(identifier)).has_value());
    });
}

StellarResult STELLAR_CALL stellar_label_find(Ptr<StellarCoreContext> context, StellarStringView name, Ptr<StellarUid> identifier, Ptr<int32_t> found)
{
    STELLAR_REQUIRE(identifier && found)

    return Guard([&]
    {
        const auto label = ContextOf(context).Labels().Get(ToView(name));
        if (label.has_value())
            *identifier = label->GetUID().ToAbi();

        return Answer(found, label.has_value());
    });
}

StellarResult STELLAR_CALL stellar_label_get_name(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<char> buffer, size_t capacity, Ptr<size_t> required)
{
    STELLAR_REQUIRE(identifier)

    return Guard([&]
    {
        const auto label = ContextOf(context).Labels().Get(ToIdentifier(identifier));
        if (!label.has_value())
            return static_cast<StellarResult>(STELLAR_RESULT_NOT_FOUND);

        return CopyText(label->GetName(), buffer, capacity, required);
    });
}

StellarResult STELLAR_CALL stellar_label_unregister(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> removed)
{
    STELLAR_REQUIRE(identifier && removed)

    return Guard([&] { return Answer(removed, ContextOf(context).Labels().Unregister(ToIdentifier(identifier))); });
}

StellarResult STELLAR_CALL stellar_label_contains(Ptr<StellarCoreContext> context, Ptr<const StellarUid> identifier, Ptr<int32_t> contained)
{
    STELLAR_REQUIRE(identifier && contained)

    return Guard([&] { return Answer(contained, ContextOf(context).Labels().Contains(ToIdentifier(identifier))); });
}

StellarResult STELLAR_CALL stellar_label_count(Ptr<StellarCoreContext> context, Ptr<size_t> count)
{
    STELLAR_REQUIRE(count)

    *count = ContextOf(context).Labels().size();
    return STELLAR_RESULT_OK;
}

// Containers

StellarResult STELLAR_CALL stellar_container_create(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> created)
{
    STELLAR_REQUIRE(container && created)

    return Guard([&] { return Answer(created, ContextOf(context).Containers().Create(ToIdentifier(container))); });
}

StellarResult STELLAR_CALL stellar_container_destroy(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> destroyed)
{
    STELLAR_REQUIRE(container && destroyed)

    return Guard([&] { return Answer(destroyed, ContextOf(context).Containers().Destroy(ToIdentifier(container))); });
}

StellarResult STELLAR_CALL stellar_container_exists(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> exists)
{
    STELLAR_REQUIRE(container && exists)

    return Guard([&] { return Answer(exists, ContextOf(context).Containers().Exists(ToIdentifier(container))); });
}

StellarResult STELLAR_CALL stellar_container_seal(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container)
{
    STELLAR_REQUIRE(container)

    return Guard([&]
    {
        ContextOf(context).Containers().Seal(ToIdentifier(container));
        return STELLAR_RESULT_OK;
    });
}

StellarResult STELLAR_CALL stellar_container_is_sealed(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> sealed)
{
    STELLAR_REQUIRE(container && sealed)

    return Guard([&] { return Answer(sealed, ContextOf(context).Containers().IsSealed(ToIdentifier(container))); });
}

StellarResult STELLAR_CALL stellar_container_add(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> item, Ptr<int32_t> added)
{
    STELLAR_REQUIRE(container && item && added)

    return Guard([&]
    {
        return Answer(added, ContextOf(context).Containers().Add(ToIdentifier(container), ToIdentifier(item)));
    });
}

StellarResult STELLAR_CALL stellar_container_remove(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> item, Ptr<int32_t> removed)
{
    STELLAR_REQUIRE(container && item && removed)

    return Guard([&]
    {
        return Answer(removed, ContextOf(context).Containers().Remove(ToIdentifier(container), ToIdentifier(item)));
    });
}

StellarResult STELLAR_CALL stellar_container_contains(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> item, Ptr<int32_t> contained)
{
    STELLAR_REQUIRE(container && item && contained)

    return Guard([&]
    {
        return Answer(contained, ContextOf(context).Containers().Contains(ToIdentifier(container), ToIdentifier(item)));
    });
}

StellarResult STELLAR_CALL stellar_container_count(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<size_t> count)
{
    STELLAR_REQUIRE(container && count)

    return Guard([&]
    {
        *count = ContextOf(context).Containers().Count(ToIdentifier(container));
        return STELLAR_RESULT_OK;
    });
}

StellarResult STELLAR_CALL stellar_container_items(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<StellarUid> buffer, size_t capacity, Ptr<size_t> required)
{
    STELLAR_REQUIRE(container)

    return Guard([&]
    {
        return CopyIdentifiers(ContextOf(context).Containers().Items(ToIdentifier(container)), buffer, capacity, required);
    });
}

StellarResult STELLAR_CALL stellar_container_clear(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<int32_t> cleared)
{
    STELLAR_REQUIRE(container && cleared)

    return Guard([&] { return Answer(cleared, ContextOf(context).Containers().Clear(ToIdentifier(container))); });
}

StellarResult STELLAR_CALL stellar_container_add_branch(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> branch, Ptr<int32_t> added)
{
    STELLAR_REQUIRE(container && branch && added)

    return Guard([&]
    {
        return Answer(added, ContextOf(context).Containers().AddBranch(ToIdentifier(container), ToIdentifier(branch)));
    });
}

StellarResult STELLAR_CALL stellar_container_remove_branch(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> branch, Ptr<int32_t> removed)
{
    STELLAR_REQUIRE(container && branch && removed)

    return Guard([&]
    {
        return Answer(removed, ContextOf(context).Containers().RemoveBranch(ToIdentifier(container), ToIdentifier(branch)));
    });
}

StellarResult STELLAR_CALL stellar_container_contains_branch(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<const StellarUid> branch, Ptr<int32_t> contained)
{
    STELLAR_REQUIRE(container && branch && contained)

    return Guard([&]
    {
        return Answer(contained, ContextOf(context).Containers().ContainsBranch(ToIdentifier(container), ToIdentifier(branch)));
    });
}

StellarResult STELLAR_CALL stellar_container_branches(Ptr<StellarCoreContext> context, Ptr<const StellarUid> container, Ptr<StellarUid> buffer, size_t capacity, Ptr<size_t> required)
{
    STELLAR_REQUIRE(container)

    return Guard([&]
    {
        return CopyIdentifiers(ContextOf(context).Containers().Branches(ToIdentifier(container)), buffer, capacity, required);
    });
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
