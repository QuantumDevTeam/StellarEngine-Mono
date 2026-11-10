#include "pch.h"
#include "Data/Registry/IdentifierRegistry.h"

#include "Data/Collections/ConcurrentUnorderedMap.h"

namespace Stellar::Native::Core::Data::Registry
{
    struct IdentifierRegistry::Impl
    {
        Collections::ConcurrentUnorderedIdentifierMap<Identifier> identifiers{64};
    };

    IdentifierRegistry::IdentifierRegistry() :
        _impl(std::make_unique<Impl>()) {}

    IdentifierRegistry::~IdentifierRegistry() = default;


    bool IdentifierRegistry::Register(Link<const Identifier> id)
    {
        return !id.IsNull() && _impl->identifiers.TryAdd(id, id);
    }

    bool IdentifierRegistry::Unregister(Link<const Identifier> id)
    {
        Identifier removed;
        return _impl->identifiers.TryRemove(id, removed);
    }

    bool IdentifierRegistry::Contains(Link<const Identifier> id) const
    {
        return _impl->identifiers.Contains(id);
    }

    std::size_t IdentifierRegistry::size() const
    {
        return _impl->identifiers.size();
    }

    std::vector<Identifier> IdentifierRegistry::Snapshot() const
    {
        return _impl->identifiers.Keys();
    }
}
