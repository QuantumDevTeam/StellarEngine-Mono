// A minimal native module: this is everything a module needs to be loaded by the engine.
#include "Abi/Module.h"

namespace
{
    StellarResult STELLAR_CALL Initialize()
    {
        return STELLAR_RESULT_OK;
    }

    void STELLAR_CALL Shutdown() {}

    constexpr char kName[] = "Stellar.Native.TestModule";
    constexpr char kVersion[] = "1.0.0";

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
