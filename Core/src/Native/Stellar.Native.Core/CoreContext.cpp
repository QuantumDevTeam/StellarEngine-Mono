#include "pch.h"
#include "CoreContext.h"

namespace Stellar::Native::Core
{
    struct CoreContext::Impl
    {
        // Declared in reverse order of dependence: modules go first when the context is destroyed
        Data::Registry::IdentifierRegistry identifiers;
        Data::Registry::LabelRegistry labels;
        Data::Registry::ContainerRegistry containers;
        Modules::ModuleRegistry modules;
    };

    CoreContext::CoreContext() :
        _impl(std::make_unique<Impl>()) {}

    CoreContext::~CoreContext() = default;

    Link<CoreContext> CoreContext::Default()
    {
        static CoreContext instance;
        return instance;
    }

    Link<Data::Registry::IdentifierRegistry> CoreContext::Identifiers() const
    {
        return _impl->identifiers;
    }

    Link<Data::Registry::LabelRegistry> CoreContext::Labels() const
    {
        return _impl->labels;
    }

    Link<Data::Registry::ContainerRegistry> CoreContext::Containers() const
    {
        return _impl->containers;
    }

    Link<Modules::ModuleRegistry> CoreContext::Modules() const
    {
        return _impl->modules;
    }
}
