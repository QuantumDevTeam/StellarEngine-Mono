#include "pch.h"
#include "CoreContext.h"

namespace Stellar::Native::Core
{
    namespace
    {
        [[nodiscard]] Ptr<Memory::Allocator> CreateAllocator()
        {
            const auto allocator = Memory::Allocator::Create();
            if (!allocator)
                throw std::bad_alloc();

            return allocator;
        }
    }

    struct CoreContext::Impl
    {
        // Declared in reverse order of dependence: modules go first when the context is destroyed, the allocator last
        Memory::AllocatorReference allocator{CreateAllocator()};
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

    Link<Memory::Allocator> CoreContext::Allocator() const
    {
        return _impl->allocator.Get();
    }

    Link<Modules::ModuleRegistry> CoreContext::Modules() const
    {
        return _impl->modules;
    }
}
