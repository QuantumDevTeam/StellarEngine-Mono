#include "Wrapper/Modules/ModuleRegistry.cli.h"

using Stellar::Core::Native::NativeCall;
using Stellar::Core::Native::NativeConvert;
using Stellar::Kernel::Interop::NativeCoreException;
using Stellar::Kernel::Interop::NativeResult;
using NativeRegistry = nc::Modules::ModuleRegistry;

namespace Stellar::Core::Modules
{
    int ModuleRegistry::Count::get()
    {
        return static_cast<int>(_context->Native().Modules().size());
    }

    NativeHandle ModuleRegistry::Load(String^ path)
    {
        return NativeConvert::ToManaged(NativeCall<&NativeRegistry::Load>(_context->Native().Modules(), NativeConvert::ToNative(path)));
    }

    bool ModuleRegistry::Unload(NativeHandle module)
    {
        return NativeCall<&NativeRegistry::Unload>(_context->Native().Modules(), NativeConvert::ToNative(module));
    }

    String^ ModuleRegistry::GetName(NativeHandle module)
    {
        const auto name = NativeCall<&NativeRegistry::GetName>(_context->Native().Modules(), NativeConvert::ToNative(module));

        if (!name.has_value())
            throw gcnew NativeCoreException(NativeResult::InvalidHandle, "The module handle is stale");

        return NativeConvert::ToManaged(*name);
    }
}
