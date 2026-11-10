#include "Wrapper/Data/Registry/ContainerRegistry.cli.h"

#include <vector>

using Stellar::Core::Native::NativeCall;
using Stellar::Core::Native::NativeConvert;
using NativeRegistry = nc::Data::Registry::ContainerRegistry;

namespace Stellar::Core::Data::Registry
{
    namespace
    {
        array<Guid>^ ToManaged(Link<const std::vector<nc::Identifier>> identifiers)
        {
            auto result = gcnew array<Guid>(static_cast<int>(identifiers.size()));

            for (int i = 0; i < result->Length; ++i)
                result[i] = NativeConvert::ToManaged(identifiers[static_cast<std::size_t>(i)]);

            return result;
        }
    }

    bool ContainerRegistry::Create(Guid container)
    {
        return NativeCall<&NativeRegistry::Create>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::Destroy(Guid container)
    {
        return NativeCall<&NativeRegistry::Destroy>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::Exists(Guid container)
    {
        return NativeCall<&NativeRegistry::Exists>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    void ContainerRegistry::Seal(Guid container)
    {
        NativeCall<&NativeRegistry::Seal>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::IsSealed(Guid container)
    {
        return NativeCall<&NativeRegistry::IsSealed>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::Add(Guid container, Guid item)
    {
        return NativeCall<&NativeRegistry::Add>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(item));
    }

    bool ContainerRegistry::Remove(Guid container, Guid item)
    {
        return NativeCall<&NativeRegistry::Remove>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(item));
    }

    bool ContainerRegistry::Contains(Guid container, Guid item)
    {
        return NativeCall<&NativeRegistry::Contains>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(item));
    }

    int ContainerRegistry::Count(Guid container)
    {
        return static_cast<int>(NativeCall<&NativeRegistry::Count>(_context->Native().Containers(), NativeConvert::ToNative(container)));
    }

    array<Guid>^ ContainerRegistry::Items(Guid container)
    {
        return ToManaged(NativeCall<&NativeRegistry::Items>(_context->Native().Containers(), NativeConvert::ToNative(container)));
    }

    bool ContainerRegistry::Clear(Guid container)
    {
        return NativeCall<&NativeRegistry::Clear>(_context->Native().Containers(), NativeConvert::ToNative(container));
    }

    bool ContainerRegistry::AddBranch(Guid container, Guid branch)
    {
        return NativeCall<&NativeRegistry::AddBranch>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(branch));
    }

    bool ContainerRegistry::RemoveBranch(Guid container, Guid branch)
    {
        return NativeCall<&NativeRegistry::RemoveBranch>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(branch));
    }

    bool ContainerRegistry::ContainsBranch(Guid container, Guid branch)
    {
        return NativeCall<&NativeRegistry::ContainsBranch>(_context->Native().Containers(), NativeConvert::ToNative(container), NativeConvert::ToNative(branch));
    }

    array<Guid>^ ContainerRegistry::Branches(Guid container)
    {
        return ToManaged(NativeCall<&NativeRegistry::Branches>(_context->Native().Containers(), NativeConvert::ToNative(container)));
    }
}
