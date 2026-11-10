# Native core and its boundaries

- [README.md](./../../README.md)

Anything C++ may ever need is written in C++. The native core owns the objects and the registries; managed code holds
an identifier or a handle and passes it back. C# is for logic, virtualization and object models on top.

```
Stellar.Core (C#)  ──  the classes it names: Stellar.Core.Identifier, Label, ContainerRegistry, ...
        │              provided by one of two wrappers, picked by Stellar.Sdk
        ├── Windows:   Stellar.Native.Core.Wrapper.Cli ──────────────────────────────┐ C++ calls
        └── elsewhere: Stellar.Native.Core.Wrapper.Managed ── P/Invoke ──┐           │
                                                Stellar.Native.Core.Wrapper.PInvoke ──┤ C++ calls
                                                                                     ▼
                                                                       Stellar.Native.Core (C++)
```

The native core therefore has two surfaces:

| Surface | Where | For |
|---|---|---|
| C++ API | the headers of `Core/src/Native/Stellar.Native.Core` | C++ code, and the C++/CLI wrapper |
| C ABI | `include/PInvoke/exports.h`, `Abi/Module.h` | the P/Invoke wrapper, native modules |

## C++ API

- **C++20 headers, C++23 inside.** `/clr` stops at C++20, so every public header compiles as C++20, and
  `HeadersCheck.cpp` proves it on each build. Sources use C++23.
- **Kernel types.** Parameters are `Link<T>` (a reference) and `Ptr<T>` (a pointer-sized, trivially copyable pointer),
  not raw `T&` and `T*`.
- **No threading headers in what C++/CLI sees.** Registries are pimpl: their mutexes live in the `.cpp`.
  `include/Core/core_interop.h` is the umbrella that is safe for a `/clr` translation unit.
- **Exceptions.** A failure is a `Failures::NativeException` with a `StellarResult` code.

| Class | What it holds |
|---|---|
| `CoreContext` | the four registries below, for one engine. `CoreContext::Default()` is the one of the process |
| `Identifier` | 16 bytes, the layout of `System.Guid` |
| `IdentifierRegistry` | the registered identifiers |
| `Label`, `LabelRegistry` | names bound to identifiers, the registry owns the text |
| `ContainerRegistry` | membership of data containers and the branches of rosters, by identifier |
| `Modules::ModuleRegistry` | loaded native modules, by handle |

## Contexts

The registries are not global: a `CoreContext` owns one of each, and every engine instance has its own. Two engines in
one process (an editor and the game it runs) register the same identifier or the same label name without seeing each
other, and destroying a context takes its registries with it, unloading its native modules.

| Side | The context of the running code |
|---|---|
| C++ | passed explicitly: `context.Identifiers().Register(id)` |
| C ABI | the first argument of every function that reaches a registry; null is `CoreContext::Default()` |
| C# | `Stellar.Core.Native.CoreContext.Current`: the context of the engine of `EngineScope.Current`, or `Default` |

`CoreContext.For(engine)` gives the context of an engine, made the first time; `CoreContext.Release(engine)` disposes
it. Stellar.Core does both through `CoreEngine`. `Identifier.CreateAndRegister()`, `Label.Create(name)` and the other
calls without a registry work in `CoreContext.Current`; the overloads that take a registry work in that one.

## Wrappers

Both wrappers define the same classes in the same namespaces, with the same members. Engine code never names a wrapper:
it names `Stellar.Core.Identifier`, and the SDK decides which assembly provides it.

| | `Cli` | `Managed` |
|---|---|---|
| Project | `Stellar.Native.Core.Wrapper.Cli` | `Stellar.Native.Core.Wrapper.Managed` + `.PInvoke` |
| Reaches the core | C++ calls | P/Invoke into flat exports, which make the C++ calls |
| Platforms | Windows | every platform |
| Toolchain | Visual Studio with C++/CLI support | the .NET SDK, plus a C++ compiler for the exports |
| Native AOT | not supported by C++/CLI | works |

`StellarNativeWrapper` selects one: `Cli` on Windows, `Managed` elsewhere and whenever `PublishAot` is set.

```bash
dotnet build -p:StellarNativeWrapper=Managed
```

The parity test (`Core/tests/Managed/Stellar.Core.Tests/WrapperTests.cs`) reads both assemblies as metadata and fails when their
public surfaces differ, and the behaviour tests run once per wrapper (`scripts/build.ps1` does both). One consequence:
no optional parameters, since C++/CLI cannot write default values to metadata. Both wrappers declare overloads.

### Exceptions in the C++/CLI wrapper

A C++ exception must not reach a managed frame. Since .NET 9 the runtime handles exceptions in managed code itself, and
a `catch` of a native type in C++/CLI code no longer sees an exception thrown by native code: it arrives as
`SEHException`. So every call into the core goes through `NativeCall` (`Wrapper/Interop.cli.h`):

```cpp
return NativeCall<&nc::Data::Registry::ContainerRegistry::Count>(registry, NativeConvert::ToNative(container));
```

It is still a plain C++ call of the C++ method. The template that makes the call and catches is defined under
`#pragma managed(off)`, and a template is compiled by the pragma state of its definition: every instantiation is native.
The failure comes back as a `NativeCoreException` with the same code and message the `Managed` wrapper produces.

## C ABI

Used by the P/Invoke exports and by native modules.

- **C only.** `extern "C"` functions and plain structures. No C++ types, no STL.
- **Fixed layout.** Every shared structure exists on both sides with the same layout, and a `static_assert` guards it.
- **Pointers are `Ptr<T>`.** Pointer-sized and trivially copyable, so for the calling convention it is a pointer.
- **Handles, not pointers to objects.** A native object is addressed by `StellarHandle`: an index and a generation.
  The one exception is `StellarCoreContext`: an opaque pointer, created and destroyed by its owner and passed with every
  registry call, where looking a handle up would cost a lock per call.
- **No exceptions.** An exception stops at `Failures::Guard` and becomes a `StellarResult`. Its message is kept per thread
  and read with `stellar_core_get_last_error`.
- **Answers are not failures.** "Already registered" or "not found by name" come back as `0` in an `int32_t` out
  parameter with `STELLAR_RESULT_OK`. A result code means the call itself failed: an unknown container, a stale handle.
- **UTF-8.** Strings are `StellarStringView`: a pointer and a size, not owned and not null-terminated.
- **Explicit ownership.** The side that allocates, frees. Text and lists come back through a caller's buffer.
- **Versioned.** Every incompatible change bumps the version.

### Shared types

| Native (`Abi/Abi.h`) | Managed | Size |
|---|---|---|
| `StellarContractVersion` | `Stellar.Kernel.Versioning.ContractVersion` | 8 |
| `StellarUid` | `System.Guid` | 16 |
| `StellarHandle` | `Stellar.Kernel.Interop.NativeHandle` | 8 |
| `StellarResult` | `Stellar.Kernel.Interop.NativeResult` | 4 |

`Stellar::Native::Core::Identifier` has the memory layout of `StellarUid`, of a Windows `GUID` and of `System.Guid`, so
identifiers cross the boundary with no conversion. Note that the first three groups of a GUID are little-endian
in memory: the bytes and the text differ in order, and `Identifier::ToString` prints what `Guid.ToString` prints.

### Buffers

A function that returns text takes `Ptr<char> buffer, size_t capacity, Ptr<size_t> required`, and one that returns a list of
identifiers takes `Ptr<StellarUid>` the same way. It always writes the needed size to `required` (for text without the
terminator) and returns `STELLAR_RESULT_BUFFER_TOO_SMALL` if the data does not fit. The caller then retries with a
bigger buffer.

### Versions

A provider satisfies a consumer when the major versions are equal and the minor version of the provider is not lower than
the required one. There are two separate contracts:

| Contract | Defined in | Between |
|---|---|---|
| P/Invoke exports (2.0: every registry call takes a context) | `include/PInvoke/exports.h` | `Stellar.Native.Core.Wrapper.Managed` and `.PInvoke` |
| Module contract | `Abi/Module.h` | The engine and a native module |

`NativeCore.EnsureAvailable` checks the first one, `ModuleRegistry` the second. The C++/CLI wrapper has no flat contract
to check: it is compiled against the headers of the core it calls.

## Native modules

A native module is a shared library that exports one function:

```cpp
#include "Abi/Module.h"

namespace
{
    constexpr char kName[] = "Stellar.Native.Sample";
    constexpr char kVersion[] = "1.0.0";

    const StellarModuleInfo kInfo = {
        sizeof(StellarModuleInfo),
        {STELLAR_MODULE_CONTRACT_MAJOR, STELLAR_MODULE_CONTRACT_MINOR},
        {kName, sizeof(kName) - 1},
        {kVersion, sizeof(kVersion) - 1},
        Initialize,   // may be null
        Shutdown,     // may be null
    };
}

STELLAR_EXTERN_C STELLAR_DLL_EXPORT const StellarModuleInfo* STELLAR_CALL StellarModule_GetInfo()
{
    return &kInfo;
}
```

The module reports the contract it was built against, and the loader checks it before running any other code of the
module. An incompatible module is a `STELLAR_RESULT_VERSION_MISMATCH`, not a crash later on.

`StellarModuleInfo` only ever grows at the end, and `structSize` tells how much of it a module knows.

## Include conventions

The native core follows the handle and umbrella rules of the Kernel:

```cpp
#include "include/Kernel/kernel.h"          // handle of the kernel
#include "include/Core/core.h"              // handle of the core
#include "include/Core/core_contracts.h"    // umbrella, for consumers
#include "include/Core/core_interop.h"      // umbrella that a /clr translation unit may include
```

A native project of a module gets the include directories, `STELLAR_KERNEL_STATIC` and the import libraries from
`Shared/Build/Stellar.Native.Module.props` (see [Modules](Modules.md)); the toolchain itself comes from
`Shared/Build/Stellar.Native.props`, shared with the core. Inside a module, paths start at the root of the module.

Headers of the C++/CLI wrapper end with `.cli.h`: MSVC also searches the folders of the open includes, and a wrapper
header named like a native one would shadow it.
