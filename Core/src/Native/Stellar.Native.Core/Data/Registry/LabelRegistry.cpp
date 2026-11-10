#include "pch.h"
#include "Data/Registry/LabelRegistry.h"

#include <mutex>
#include <shared_mutex>
#include <string>
#include <unordered_map>

namespace Stellar::Native::Core::Data::Registry
{
    struct LabelRegistry::Impl
    {
        // One lock for both maps: registering a label must be atomic across them
        mutable std::shared_mutex mutex;
        std::unordered_map<Identifier, UniquePtr<const std::string>> byId;
        std::unordered_map<std::string_view, Identifier> byName;

        bool EraseLocked(Link<const Identifier> id)
        {
            const auto it = byId.find(id);
            if (it == byId.end())
                return false;

            // The key of byName views the stored string, so it goes first
            byName.erase(*it->second);
            byId.erase(it);
            return true;
        }
    };

    LabelRegistry::LabelRegistry() :
        _impl(std::make_unique<Impl>()) {}

    LabelRegistry::~LabelRegistry() = default;


    std::optional<Label> LabelRegistry::Register(std::string_view name, Link<const Identifier> id)
    {
        if (name.empty() || id.IsNull())
            return std::nullopt;

        std::unique_lock lock(_impl->mutex);

        if (_impl->byName.contains(name) || _impl->byId.contains(id))
            return std::nullopt;

        auto stored = std::make_unique<const std::string>(name);
        const std::string_view view = *stored;

        _impl->byId.emplace(id, std::move(stored));
        _impl->byName.emplace(view, id);

        return Label{view, id};
    }

    std::optional<Label> LabelRegistry::Get(Link<const Identifier> id) const
    {
        std::shared_lock lock(_impl->mutex);

        const auto it = _impl->byId.find(id);
        if (it == _impl->byId.end())
            return std::nullopt;

        return Label{*it->second, id};
    }

    std::optional<Label> LabelRegistry::Get(std::string_view name) const
    {
        std::shared_lock lock(_impl->mutex);

        const auto it = _impl->byName.find(name);
        if (it == _impl->byName.end())
            return std::nullopt;

        return Label{it->first, it->second};
    }

    bool LabelRegistry::Unregister(Link<const Identifier> id)
    {
        std::unique_lock lock(_impl->mutex);
        return _impl->EraseLocked(id);
    }

    bool LabelRegistry::Unregister(std::string_view name)
    {
        std::unique_lock lock(_impl->mutex);

        const auto it = _impl->byName.find(name);
        if (it == _impl->byName.end())
            return false;

        return _impl->EraseLocked(Identifier(it->second));
    }

    bool LabelRegistry::Contains(Link<const Identifier> id) const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->byId.contains(id);
    }

    std::size_t LabelRegistry::size() const
    {
        std::shared_lock lock(_impl->mutex);
        return _impl->byId.size();
    }
}
