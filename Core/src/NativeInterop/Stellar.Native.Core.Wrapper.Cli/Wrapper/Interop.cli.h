// StellarEngine
// Copyright (c) 2026 QuantumDevTeam
// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0

/// @file Wrapper/Interop.cli.h
/// @brief Conversions between the native core and .NET, shared by the classes of this wrapper.
///
/// The managed classes live in `Stellar.Core` and the native ones in `Stellar::Native::Core`. Inside a managed
/// namespace an unqualified `Native` or `Core` is ambiguous, so native types are always reached through `nc`.
#pragma once

#include "include/Kernel/kernel.h"
#include "include/Core/core.h"
#include "include/Core/core_interop.h"

#include <cstring>
#include <exception>
#include <string>
#include <string_view>
#include <type_traits>
#include <new>

namespace nc = ::Stellar::Native::Core;

namespace Stellar::Core::Native
{
    using namespace System;
    using namespace System::Text;

    /// Conversions of identifiers, handles and text.
    private ref class NativeConvert abstract sealed
    {
    public:
        static nc::Identifier ToNative(Guid uid)
        {
            pin_ptr<Guid> pinned = &uid;

            nc::Identifier::Bytes bytes;
            std::memcpy(bytes.data(), pinned, bytes.size());

            return nc::Identifier(bytes);
        }

        static Guid ToManaged(Link<const nc::Identifier> id)
        {
            Guid uid;
            pin_ptr<Guid> pinned = &uid;

            std::memcpy(pinned, id.GetData().data(), sizeof(Guid));
            return uid;
        }

        static StellarHandle ToNative(Stellar::Kernel::Interop::NativeHandle handle)
        {
            return StellarHandle{handle.Index, handle.Generation};
        }

        static Stellar::Kernel::Interop::NativeHandle ToManaged(StellarHandle handle)
        {
            return Stellar::Kernel::Interop::NativeHandle(handle.index, handle.generation);
        }

        static std::string ToNative(String^ text)
        {
            if (String::IsNullOrEmpty(text))
                return {};

            array<Byte>^ bytes = Encoding::UTF8->GetBytes(text);
            pin_ptr<Byte> data = &bytes[0];

            return std::string(reinterpret_cast<const char*>(static_cast<Byte*>(data)), bytes->Length);
        }

        static String^ ToManaged(std::string_view text)
        {
            return text.empty()
                       ? String::Empty
                       : Encoding::UTF8->GetString(
                           reinterpret_cast<unsigned char*>(const_cast<char*>(text.data())), static_cast<int>(text.size()));
        }
    };
}

/*
    Calls into the native core.

    A C++ exception must never reach a managed frame: since .NET 9 the runtime handles exceptions in managed code
    itself, and a catch of a native type in C++/CLI code no longer sees an exception thrown by native code (it
    arrives as SEHException). So the call and its catch both live in native code. A template is compiled as
    managed or native by the #pragma state where it is defined, not where it is used: every instantiation of
    InvokeMember below is native, wherever NativeCall is written.

        NativeCall<&nc::Data::Registry::ContainerRegistry::Count>(registry, id)

    is a plain C++ call of the C++ method, nothing flat in between.
*/
#pragma managed(push, off)
namespace Stellar::Core::Native::Detail
{
    /// What a native call ended with.
    struct NativeOutcome
    {
        StellarResult result = STELLAR_RESULT_OK;
        std::string message;
    };

    /// Calls `(object.*Method)(arguments...)` and catches whatever it throws, in native code.
    template <auto Method, typename Result, typename Object, typename... Arguments>
    NativeOutcome InvokeMember(Link<Object> object, Ptr<Result> result, const Arguments&... arguments) noexcept
    {
        try
        {
            if constexpr (std::is_void_v<Result>)
                (object.*Method)(arguments...);
            else
                *result = (object.*Method)(arguments...);

            return {};
        }
        catch (Link<const nc::Failures::NativeException> exception)
        {
            return {exception.GetResult(), exception.what()};
        }
        catch (Link<const std::bad_alloc>)
        {
            return {STELLAR_RESULT_OUT_OF_MEMORY, "Out of native memory"};
        }
        catch (Link<const std::exception> exception)
        {
            return {STELLAR_RESULT_NATIVE_EXCEPTION, exception.what()};
        }
        catch (...)
        {
            return {STELLAR_RESULT_UNKNOWN, "Unknown native exception"};
        }
    }
}
#pragma managed(pop)

namespace Stellar::Core::Native
{
    /// Turns a failed native call into a NativeCoreException.
    inline void ThrowIfFailed(Link<const Detail::NativeOutcome> outcome)
    {
        if (outcome.result != STELLAR_RESULT_OK)
            throw gcnew ::Stellar::Kernel::Interop::NativeCoreException(
                static_cast<::Stellar::Kernel::Interop::NativeResult>(outcome.result),
                NativeConvert::ToManaged(outcome.message));
    }

    /// Calls a method of the native core; a native failure comes back as a NativeCoreException.
    template <auto Method, typename Object, typename... Arguments>
    auto NativeCall(Link<Object> object, const Arguments&... arguments)
    {
        using Result = std::remove_cvref_t<decltype((object.*Method)(arguments...))>;

        if constexpr (std::is_void_v<Result>)
        {
            ThrowIfFailed(Detail::InvokeMember<Method, void>(object, nullptr, arguments...));
        }
        else
        {
            Result result{};
            ThrowIfFailed(Detail::InvokeMember<Method, Result>(object, &result, arguments...));
            return result;
        }
    }
}
