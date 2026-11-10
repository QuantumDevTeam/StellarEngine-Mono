#include "Wrapper/Native/CoreContext.cli.h"
#include "Wrapper/Data/Registry/IdentifierRegistry.cli.h"
#include "Wrapper/Data/Registry/LabelRegistry.cli.h"
#include "Wrapper/Data/Registry/ContainerRegistry.cli.h"
#include "Wrapper/Modules/ModuleRegistry.cli.h"

using Stellar::Kernel::Lifecycle::EngineScope;
using Stellar::Kernel::Lifecycle::IEngineInstance;

namespace Stellar::Core::Native
{
    CoreContext::CoreContext(nc::CoreContext* native, bool owned) :
        _native(native),
        _owned(owned)
    {
        _identifiers = gcnew Data::Registry::IdentifierRegistry(this);
        _labels = gcnew Data::Registry::LabelRegistry(this);
        _containers = gcnew Data::Registry::ContainerRegistry(this);
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
        auto engine = EngineScope::Current;
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

    Data::Registry::IdentifierRegistry^ CoreContext::Identifiers::get() { return _identifiers; }

    Data::Registry::LabelRegistry^ CoreContext::Labels::get() { return _labels; }

    Data::Registry::ContainerRegistry^ CoreContext::Containers::get() { return _containers; }

    ::Stellar::Core::Modules::ModuleRegistry^ CoreContext::Modules::get() { return _modules; }

    nc::CoreContext& CoreContext::Native()
    {
        if (_native == nullptr)
            throw gcnew ObjectDisposedException("CoreContext");

        return *_native;
    }
}
