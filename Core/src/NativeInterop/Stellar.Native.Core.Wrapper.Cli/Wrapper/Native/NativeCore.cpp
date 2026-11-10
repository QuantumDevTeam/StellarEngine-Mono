#include "Wrapper/Native/NativeCore.cli.h"

namespace Stellar::Core::Native
{
    bool NativeCore::IsAvailable::get()
    {
        try
        {
            EnsureAvailable();
            return true;
        }
        catch (IO::FileNotFoundException^)
        {
            return false;
        }
    }

    void NativeCore::EnsureAvailable()
    {
        // Linked at load time: reaching here means Stellar.Native.Core.dll was found. Touch it to be sure.
        (void)nc::CoreContext::Default().Identifiers().size();
    }
}
