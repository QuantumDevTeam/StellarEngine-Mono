#include "Wrapper/Identifier.cli.h"
#include "Wrapper/Data/Registry/IdentifierRegistry.cli.h"
#include "Wrapper/Native/CoreContext.cli.h"

using namespace Stellar::Kernel::Data::Registry;
using Stellar::Core::Native::NativeCall;
using Stellar::Core::Native::NativeConvert;

namespace Stellar::Core
{
    namespace
    {
        IRegistry<Identifier^>^ RegistryOf(IQuantumObject^ registry)
        {
            Object^ target = registry == nullptr ? Native::CoreContext::Current->Identifiers : registry;
            auto typed = dynamic_cast<IRegistry<Identifier^>^>(target);

            if (typed == nullptr)
                throw gcnew ArgumentException(target->GetType()->Name + " cannot store identifiers", "registry");

            return typed;
        }
    }

    Identifier::Identifier(Guid uid) :
        _uid(uid) {}

    Guid Identifier::UID::get()
    {
        return _uid;
    }

    bool Identifier::IsRegistered::get()
    {
        return Native::CoreContext::Current->Identifiers->Exists(this);
    }

    void Identifier::Register()
    {
        Register(nullptr);
    }

    void Identifier::Register(IQuantumObject^ registry)
    {
        RegistryOf(registry)->Register(this);
    }

    void Identifier::Unregister()
    {
        Unregister(nullptr);
    }

    void Identifier::Unregister(IQuantumObject^ registry)
    {
        RegistryOf(registry)->Pop(this);
    }

    Identifier::~Identifier()
    {
        Unregister();
    }

    bool Identifier::Equals(IIdentifier^ other)
    {
        return other != nullptr && _uid == other->UID;
    }

    bool Identifier::Equals(Object^ obj)
    {
        return Equals(dynamic_cast<IIdentifier^>(obj));
    }

    int Identifier::GetHashCode()
    {
        return _uid.GetHashCode();
    }

    String^ Identifier::ToCanonicalString()
    {
        const nc::Identifier native = NativeConvert::ToNative(_uid);
        return NativeConvert::ToManaged(NativeCall<&nc::Identifier::ToString>(native));
    }

    String^ Identifier::ToString()
    {
        return "Identifier#" + _uid.ToString();
    }

    Identifier^ Identifier::Create()
    {
        return gcnew Identifier(NativeConvert::ToManaged(nc::Identifier::Create()));
    }

    Identifier^ Identifier::CreateAndRegister()
    {
        Identifier^ id = Create();
        id->Register();
        return id;
    }

    Identifier^ Identifier::Parse(String^ text)
    {
        const auto parsed = nc::Identifier::Parse(NativeConvert::ToNative(text));
        return parsed.has_value() ? gcnew Identifier(NativeConvert::ToManaged(*parsed)) : nullptr;
    }

    Identifier^ Identifier::Get(Guid data)
    {
        return Native::CoreContext::Current->Identifiers->Get(data);
    }

    Identifier^ Identifier::Get(array<Byte>^ data)
    {
        return Get(Guid(data));
    }

    Identifier^ Identifier::Get(String^ data)
    {
        return Get(Guid(data));
    }

    Identifier^ Identifier::Get(IIdentifier^ data)
    {
        return Get(data->UID);
    }
}
