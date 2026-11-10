#include "pch.h"
#include "Data/Registry/ContainerRegistry.h"

#include <mutex>
#include <shared_mutex>
#include <unordered_map>
#include <unordered_set>

#include "Failures/NativeException.h"

namespace Stellar::Native::Core::Data::Registry
{
    namespace
    {
        struct Container
        {
            bool sealed = false;
            std::unordered_set<Identifier> items;
            std::unordered_set<Identifier> branches;
        };

        [[nodiscard]] std::vector<Identifier> ToVector(Link<const std::unordered_set<Identifier>> set)
        {
            return {set.begin(), set.end()};
        }
    }

    struct ContainerRegistry::Impl
    {
        mutable std::shared_mutex mutex;
        std::unordered_map<Identifier, Container> containers;

        [[nodiscard]] Link<Container> Find(Link<const Identifier> id)
        {
            const auto it = containers.find(id);

            if (it == containers.end())
                throw Failures::NativeException(std::format("Container {} does not exist", id.ToString()),
                                                STELLAR_RESULT_NOT_FOUND);

            return it->second;
        }

        [[nodiscard]] Link<const Container> Find(Link<const Identifier> id) const
        {
            return const_cast<Link<Impl>>(*this).Find(id);
        }
    };

    ContainerRegistry::ContainerRegistry() :
        _impl(std::make_unique<Impl>()) {}

    ContainerRegistry::~ContainerRegistry() = default;


    bool ContainerRegistry::Create(Link<const Identifier> container)
    {
        if (container.IsNull())
            return false;

        std::unique_lock lock(_impl->mutex);
        return _impl->containers.try_emplace(container).second;
    }

    bool ContainerRegistry::Destroy(Link<const Identifier> container)
    {
        std::unique_lock lock(_impl->mutex);

        if (_impl->containers.erase(container) == 0)
            return false;

        // A destroyed container must not stay reachable as a branch of another one
        for (auto& [id, other] : _impl->containers)
            other.branches.erase(container);

        return true;
    }

    bool ContainerRegistry::Exists(Link<const Identifier> container) const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->containers.contains(container);
    }

    void ContainerRegistry::Seal(Link<const Identifier> container)
    {
        std::unique_lock lock(_impl->mutex);
        _impl->Find(container).sealed = true;
    }

    bool ContainerRegistry::IsSealed(Link<const Identifier> container) const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->Find(container).sealed;
    }

    bool ContainerRegistry::Add(Link<const Identifier> container, Link<const Identifier> item)
    {
        std::unique_lock lock(_impl->mutex);
        Link<Container> found = _impl->Find(container);

        return !found.sealed && found.items.insert(item).second;
    }

    bool ContainerRegistry::Remove(Link<const Identifier> container, Link<const Identifier> item)
    {
        std::unique_lock lock(_impl->mutex);
        Link<Container> found = _impl->Find(container);

        return !found.sealed && found.items.erase(item) != 0;
    }

    bool ContainerRegistry::Contains(Link<const Identifier> container, Link<const Identifier> item) const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->Find(container).items.contains(item);
    }

    std::size_t ContainerRegistry::Count(Link<const Identifier> container) const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->Find(container).items.size();
    }

    std::vector<Identifier> ContainerRegistry::Items(Link<const Identifier> container) const
    {
        std::shared_lock lock(_impl->mutex);
        return ToVector(_impl->Find(container).items);
    }

    bool ContainerRegistry::Clear(Link<const Identifier> container)
    {
        std::unique_lock lock(_impl->mutex);
        Link<Container> found = _impl->Find(container);

        if (found.sealed)
            return false;

        found.items.clear();
        return true;
    }

    bool ContainerRegistry::AddBranch(Link<const Identifier> container, Link<const Identifier> branch)
    {
        std::unique_lock lock(_impl->mutex);
        Link<Container> found = _impl->Find(container);

        (void)_impl->Find(branch);

        return container != branch && found.branches.insert(branch).second;
    }

    bool ContainerRegistry::RemoveBranch(Link<const Identifier> container, Link<const Identifier> branch)
    {
        std::unique_lock lock(_impl->mutex);
        return _impl->Find(container).branches.erase(branch) != 0;
    }

    bool ContainerRegistry::ContainsBranch(Link<const Identifier> container, Link<const Identifier> branch) const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->Find(container).branches.contains(branch);
    }

    std::vector<Identifier> ContainerRegistry::Branches(Link<const Identifier> container) const
    {
        std::shared_lock lock(_impl->mutex);
        return ToVector(_impl->Find(container).branches);
    }

    std::size_t ContainerRegistry::size() const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->containers.size();
    }
}
