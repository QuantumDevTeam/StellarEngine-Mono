// The native part of {{ModuleId}}: what the engine needs to load it, and the native core to build on.
#include "include/Kernel/kernel.h"
#include "include/Core/core.h"
#include "include/Core/core_contracts.h"
#include "Abi/Module.h"

namespace
{
    StellarResult STELLAR_CALL Initialize()
    {
        // The native core is ready here: its registries, identifiers and modules
        (void)Stellar::Native::Core::Identifier::Create();
        return STELLAR_RESULT_OK;
    }

    void STELLAR_CALL Shutdown()
    {
    }

    constexpr char kName[] = "{{ModuleNativeId}}";
    constexpr char kVersion[] = "{{ModuleVersion}}";

    const StellarModuleInfo kInfo = {
        sizeof(StellarModuleInfo),
        {STELLAR_MODULE_CONTRACT_MAJOR, STELLAR_MODULE_CONTRACT_MINOR},
        {kName, sizeof(kName) - 1},
        {kVersion, sizeof(kVersion) - 1},
        Initialize,
        Shutdown,
    };
}

STELLAR_EXTERN_C STELLAR_DLL_EXPORT const StellarModuleInfo* STELLAR_CALL StellarModule_GetInfo()
{
    return &kInfo;
}
