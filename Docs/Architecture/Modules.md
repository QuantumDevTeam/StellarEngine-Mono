# Modules

- [README.md](./../../README.md)

A module is a folder with a [`<Id>.stellar.module`](../Formats/ModuleDescriptor.md) file. Everything else follows from
it: the installation finds it, the SDK builds it, the analyzers keep its architecture, the packages carry it. The rule
of the whole engine applies: everything that can be automatic is, and everything automatic can be changed.

## The life of a module

```bash
dotnet stellar modules new Managed Stellar.Logging --level CoreModule -d "Logging for the engine"
dotnet stellar modules new Native Stellar.Graphic --level HighLevel
# Stellar.Graphic.stellar.module: "Dependencies": ["Stellar.Logging"]
dotnet stellar modules sync
dotnet stellar modules build --tests --pack
```

| Step | What happens |
|---|---|
| `modules new` | Renders `Shared/Templates/Modules/<template>` into `modules/<Id>/`, and syncs |
| `modules sync` | Finds the modules, applies the manifest, checks the rules, writes the catalog |
| `modules build` | For each module in load order, with the dependencies of the selected ones: native projects (MSBuild of Visual Studio), the managed project, the tests, the package |
| `modules list` | The modules in load order, where each came from, how it is linked |

`modules build --plan` shows the commands without running them; `--no-native`, `--wrapper Managed`,
`-c Release` change what is built.

A module developed in its own repository stays there:

```bash
dotnet stellar modules add F:/project/CS/Stellar.Logging
```

It is then part of the installation exactly like a folder of `modules/`, and gets a `NuGet.config` pointing at the
local feed if it has none.

## What a module project gets

The project of a module is nearly empty:

```xml
<Project Sdk="Stellar.Sdk">
</Project>
```

The SDK finds the project in the catalog by `$(StellarModuleId)` (the assembly name), and fills in what the project
left empty:

| What | From | Switch it off |
|---|---|---|
| `StellarSdkUsage` | the level: `CoreModule` is `EngineCoreModule`, `Runtime` and `HighLevel` are `EngineModule`, `EndPoint` is `Module` | `StellarModuleAutoUsage=false`, or set it |
| `Version` | the descriptor | `StellarModuleAutoVersion=false`, or set it |
| `ModuleDescription`, `PackageProjectUrl` | `Description`, `Repository` | set them |
| `StellarDependency` items | `Dependencies` | `StellarModuleAutoDependencies=false`, or `<StellarDependency Remove="..."/>` |
| references to `Stellar.Kernel`, `Stellar.Core` | always | `StellarImplicitKernelReference=false`, `StellarImplicitCoreReference=false` |
| the wrapper of the native core | `StellarNativeWrapper` | `StellarReferenceNativeWrapper=false` |
| the DLLs of its native projects, next to the assembly and in the package | `NativeProjects` | `StellarModuleCopyNativeLibraries=false` |
| `[AssemblyMetadata]` with its id, level and dependencies | | `StellarStampMetadata=false` |

Every property: [Sdk.Properties](../Reference/Sdk.Properties.md).

## How references are made

Each `StellarDependency` is looked up in the catalog, and becomes:

| | `Link` of the manifest | `StellarPrecompiled` |
|---|---|---|
| `ProjectReference` | `Sources` | `false` (the default of Debug) |
| `PackageReference` | `Package` | `true` (the default of the other configurations) |

So a game in Debug builds against the sources of the modules, in Release against their packages, and one module can
be pinned either way for the whole installation.

## At runtime

The build of an application writes the modules it loads into its runtime configuration: every module it depends
on, directly or not, every module after the ones it depends on. `Stellar.Runtime` loads them in that order, each module
above the core into a load context of its own, starts their entry points level by level, then the application. See
[Runtime](Runtime.md).

## The native part

A native project of a module imports two shared files:

```xml
<Import Project="$(StellarRoot)Shared\Build\Stellar.Native.props"/>          <!-- the toolchain of the engine -->
<Import Project="$(StellarRoot)Shared\Build\Stellar.Native.Module.props"/>   <!-- the headers and the libraries of the native core -->
```

It builds into `artifacts/bin/<project>/<configuration>/`, like every project of the installation, links the native
core as it was built (it never rebuilds the engine), and exports `StellarModule_GetInfo` (see [Native core](NativeAbi.md)). The managed project of the module
then gets the DLL by itself.

`StellarNativeLinkCore=false` makes a plain C ABI module, with the Kernel headers only.

## The rules, and who checks them

| Rule | Checked by |
|---|---|
| A module depends only on strictly lower levels | `modules sync` (descriptors), STLR0201 (the compiled references) |
| A module uses only the modules its descriptor lists | STLR0202 |
| The descriptor names the project it belongs to | STLR0203, and `modules sync` for the file name |
| The descriptor and the project file are well-formed | STLR0301 |
| The catalog knows the module, and is up to date | STLRB101, STLRB102 at build time |
| The project says what it is | STLRB001 to STLRB003 |

Every diagnostic: [Diagnostics](../Reference/Diagnostics.md). Their severity is set in `.editorconfig`, like any
Roslyn rule.

## Settings of the installation

`stellar install` creates `config/`, and never overwrites it:

| File | Applies to | When |
|---|---|---|
| `config/Stellar.Sdk.props` | every SDK project of the installation | before the project body: defaults the project may still change |
| `config/Stellar.Sdk.targets` | every SDK project of the installation | after everything: steps of your own, decisions by kind of project |
| `config/Stellar.Native.props` | every native project, of the core and of the modules | after the defaults of `Stellar.Native.props` |

The order of precedence, from the strongest: the project, `config/`, the defaults of the SDK. For a module: the
project, the manifest, the descriptor, the conventions.
