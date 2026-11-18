#include "Wrapper/Native/CoreContext.cli.h"
#include "Wrapper/Modules/ModuleRegistry.cli.h"
#include "Wrapper/Native/NativeAllocator.cli.h"

using Stellar::Kernel::Lifecycle::EngineScope;
using Stellar::Kernel::Lifecycle::IEngineInstance;

namespace Stellar::Core::Native
{
    CoreContext::CoreContext(nc::CoreContext* native, bool owned) :
        _native(native),
        _owned(owned)
    {
        _modules = gcnew ::Stellar::Core::Modules::ModuleRegistry(this);
    }

    CoreContext::CoreContext() :
        CoreContext(new nc::CoreContext(), true) {}

    CoreContext::~CoreContext()
    {
        this->!CoreContext();
        GC::SuppressFinalize(this);
    }

    CoreContext::!CoreContext()
    {
        if (_owned && _native != nullptr)
        {
            delete _native;
            _native = nullptr;
        }
    }

    CoreContext^ CoreContext::Current::get()
    {
        auto engine = EngineScope::Resolve();
        return engine == nullptr ? _default : For(engine);
    }

    CoreContext^ CoreContext::For(IEngineInstance^ engine)
    {
        if (engine == nullptr)
            throw gcnew ArgumentNullException("engine");

        return _engines->GetValue(engine, gcnew ConditionalWeakTable<IEngineInstance^, CoreContext^>::CreateValueCallback(
                                      &CoreContext::CreateFor));
    }

    CoreContext^ CoreContext::CreateFor(IEngineInstance^)
    {
        return gcnew CoreContext();
    }

    bool CoreContext::Release(IEngineInstance^ engine)
    {
        CoreContext^ context;
        if (engine == nullptr || !_engines->TryGetValue(engine, context))
            return false;

        _engines->Remove(engine);
        delete context;
        return true;
    }

    ::Stellar::Core::Modules::ModuleRegistry^ CoreContext::Modules::get() { return _modules; }

    NativeAllocator^ CoreContext::Allocator::get()
    {
        if (_allocator == nullptr)
        {
            auto allocator = gcnew NativeAllocator(&Native().Allocator(), false);
            System::Threading::Interlocked::CompareExchange<NativeAllocator^>(_allocator, allocator, nullptr);
        }

        return _allocator;
    }

    nc::CoreContext& CoreContext::Native()
    {
        if (_native == nullptr)
            throw gcnew ObjectDisposedException("CoreContext");

        return *_native;
    }
}
