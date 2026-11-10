#include "Wrapper/Data/Registry/LabelRegistry.cli.h"

using Stellar::Core::Native::NativeCall;
using Stellar::Core::Native::NativeConvert;
using NativeRegistry = nc::Data::Registry::LabelRegistry;

namespace
{
    // The native registry looks labels up by identifier and by name: a member pointer must say which one
    constexpr auto NativeGetById = static_cast<std::optional<nc::Label> (NativeRegistry::*)(Link<const nc::Identifier>) const>(&NativeRegistry::Get);
    constexpr auto NativeGetByName = static_cast<std::optional<nc::Label> (NativeRegistry::*)(std::string_view) const>(&NativeRegistry::Get);
    constexpr auto NativeUnregisterById = static_cast<bool (NativeRegistry::*)(Link<const nc::Identifier>)>(&NativeRegistry::Unregister);
}

namespace Stellar::Core::Data::Registry
{
    bool LabelRegistry::Exists(IIdentifier^ id)
    {
        if (id == nullptr)
            return false;

        return NativeCall<&NativeRegistry::Contains>(_context->Native().Labels(), NativeConvert::ToNative(id->UID));
    }

    bool LabelRegistry::Register(Label^ obj)
    {
        if (obj == nullptr)
            throw gcnew ArgumentNullException("obj");

        return NativeCall<&NativeRegistry::Register>(_context->Native().Labels(), NativeConvert::ToNative(obj->Name), NativeConvert::ToNative(obj->UID->UID)).has_value();
    }

    Label^ LabelRegistry::Get(IIdentifier^ id)
    {
        if (id == nullptr)
            return nullptr;

        const auto label = NativeCall<NativeGetById>(_context->Native().Labels(), NativeConvert::ToNative(id->UID));
        return label.has_value() ? Label::FromNative(id->UID, NativeConvert::ToManaged(label->GetName())) : nullptr;
    }

    Label^ LabelRegistry::GetByName(String^ name)
    {
        if (String::IsNullOrEmpty(name))
            return nullptr;

        const auto label = NativeCall<NativeGetByName>(_context->Native().Labels(), NativeConvert::ToNative(name));
        return label.has_value() ? Label::FromNative(NativeConvert::ToManaged(label->GetUID()), name) : nullptr;
    }

    Label^ LabelRegistry::Pop(IIdentifier^ id)
    {
        Label^ label = Get(id);
        if (label == nullptr)
            return nullptr;

        return NativeCall<NativeUnregisterById>(_context->Native().Labels(), NativeConvert::ToNative(id->UID)) ? label : nullptr;
    }

    int LabelRegistry::Size::get()
    {
        return static_cast<int>(_context->Native().Labels().size());
    }

    ICollection<IIdentifier^>^ LabelRegistry::Keys::get()
    {
        throw gcnew NotSupportedException("Labels are looked up, not enumerated");
    }

    ICollection<Label^>^ LabelRegistry::Values::get()
    {
        throw gcnew NotSupportedException("Labels are looked up, not enumerated");
    }
}
