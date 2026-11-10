#include "Wrapper/Data/Registry/IdentifierRegistry.cli.h"

using Stellar::Core::Native::NativeCall;
using Stellar::Core::Native::NativeConvert;
using NativeRegistry = nc::Data::Registry::IdentifierRegistry;

namespace Stellar::Core::Data::Registry
{
    bool IdentifierRegistry::Exists(IIdentifier^ id)
    {
        if (id == nullptr)
            return false;

        return NativeCall<&NativeRegistry::Contains>(_context->Native().Identifiers(), NativeConvert::ToNative(id->UID));
    }

    bool IdentifierRegistry::Register(Identifier^ obj)
    {
        if (obj == nullptr)
            throw gcnew ArgumentNullException("obj");

        return NativeCall<&NativeRegistry::Register>(_context->Native().Identifiers(), NativeConvert::ToNative(obj->UID));
    }

    Identifier^ IdentifierRegistry::Get(IIdentifier^ id)
    {
        return id == nullptr ? nullptr : Get(id->UID);
    }

    Identifier^ IdentifierRegistry::Get(Guid uid)
    {
        return NativeCall<&NativeRegistry::Contains>(_context->Native().Identifiers(), NativeConvert::ToNative(uid)) ? gcnew Identifier(uid) : nullptr;
    }

    Identifier^ IdentifierRegistry::Pop(IIdentifier^ id)
    {
        if (id == nullptr)
            return nullptr;

        return NativeCall<&NativeRegistry::Unregister>(_context->Native().Identifiers(), NativeConvert::ToNative(id->UID)) ? gcnew Identifier(id->UID) : nullptr;
    }

    int IdentifierRegistry::Size::get()
    {
        return static_cast<int>(_context->Native().Identifiers().size());
    }

    ICollection<IIdentifier^>^ IdentifierRegistry::Keys::get()
    {
        auto keys = gcnew List<IIdentifier^>();

        for each (Identifier^ id in Values)
            keys->Add(id);

        return keys;
    }

    ICollection<Identifier^>^ IdentifierRegistry::Values::get()
    {
        const auto snapshot = NativeCall<&NativeRegistry::Snapshot>(_context->Native().Identifiers());
        auto values = gcnew List<Identifier^>(static_cast<int>(snapshot.size()));

        for (Link<const nc::Identifier> id : snapshot)
            values->Add(gcnew Identifier(NativeConvert::ToManaged(id)));

        return values;
    }
}
