#include "pch.h"
#include "Failures/Guard.h"

namespace Stellar::Native::Core::Failures
{
    namespace
    {
        thread_local std::string lastError;
    }

    void SetLastError(std::string_view message)
    {
        try
        {
            lastError.assign(message);
        }
        catch (...)
        {
            // Reporting a failure must never become a failure of its own
            lastError.clear();
        }
    }

    Link<const std::string> GetLastError() noexcept
    {
        return lastError;
    }
}
