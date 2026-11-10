#include "pch.h"
#include "Modules/SharedLibrary.h"

#include <utility>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#else
#include <dlfcn.h>
#endif

#include "Failures/NativeException.h"

namespace Stellar::Native::Core::Modules
{
    SharedLibrary::~SharedLibrary()
    {
        Close();
    }

    SharedLibrary::SharedLibrary(SharedLibrary&& other) noexcept :
        _handle(std::exchange(other._handle, nullptr)) {}

    SharedLibrary& SharedLibrary::operator=(SharedLibrary&& other) noexcept
    {
        if (this != &other)
        {
            Close();
            _handle = std::exchange(other._handle, nullptr);
        }

        return *this;
    }

    SharedLibrary SharedLibrary::Open(std::string_view path)
    {
        SharedLibrary library;

#ifdef _WIN32
        const int length = ::MultiByteToWideChar(CP_UTF8, 0, path.data(), static_cast<int>(path.size()), nullptr, 0);

        std::wstring widePath(static_cast<std::size_t>(length), L'\0');
        ::MultiByteToWideChar(CP_UTF8, 0, path.data(), static_cast<int>(path.size()), widePath.data(), length);

        library._handle = ::LoadLibraryW(widePath.c_str());

        if (!library._handle)
            throw Failures::NativeException(
                std::format("Cannot load {}: system error {}", path, ::GetLastError()), STELLAR_RESULT_NOT_FOUND);
#else
        library._handle = ::dlopen(std::string(path).c_str(), RTLD_NOW);

        if (!library._handle)
            throw Failures::NativeException(std::format("Cannot load {}: {}", path, ::dlerror()), STELLAR_RESULT_NOT_FOUND);
#endif

        return library;
    }

    Ptr<void> SharedLibrary::FindSymbol(Link<const std::string> name) const noexcept
    {
#ifdef _WIN32
        return reinterpret_cast<void*>(::GetProcAddress(static_cast<HMODULE>(_handle.get()), name.c_str()));
#else
        return ::dlsym(_handle.get(), name.c_str());
#endif
    }

    void SharedLibrary::Close() noexcept
    {
        if (!_handle)
            return;

#ifdef _WIN32
        ::FreeLibrary(static_cast<HMODULE>(_handle.get()));
#else
        ::dlclose(_handle.get());
#endif

        _handle = nullptr;
    }
}
