# SDK

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

`Stellar.Sdk` is the MSBuild SDK every module and game is built with. It decides what a project references and adds the
engine steps to the build. It does not define engine behavior.

## A project

```xml
<Project Sdk="Stellar.Sdk">
    <PropertyGroup>
        <StellarSdkUsage>ExecutableModule</StellarSdkUsage>
        <StellarDependencies>Stellar.FileSystem;Stellar.Logging</StellarDependencies>
    </PropertyGroup>
</Project>
```

The modules a project uses are a property, `StellarDependencies`: the SDK makes the dependencies from it in its own
targets, and an IDE shows nothing of it as a file. A module may also be declared as a package,
`<PackageReference Include="Stellar.FileSystem"/>`, by hand or from the NuGet window of the IDE (the feed `StellarLocal`
of `NuGet.config`): a package the catalog knows is a module, of the version of the installation, linked as the package or
as the sources; its own `Version`, if any, does not count. `StellarDependency` items still work, module descriptors make
them, but Rider shows an item written in a project file as a file of the project, a missing one.

The SDK version is pinned once, in `global.json`, not in every project:

```json
{ "msbuild-sdks": { "Stellar.Sdk": "0.4.0" } }
```

The SDK itself comes from the local feed through `NuGet.config`. `stellar project new` and `stellar modules new`
generate both files; a project under the root of the installation also finds the `NuGet.config` of the root.

A module project needs even less: it is nearly empty, and the SDK takes its kind, version and dependencies from its
descriptor. See [Modules](https://quantumdevteam.github.io/StellarEngine-Mono/guide/Modules.html). Every property: [Sdk.Properties](../Reference/Sdk.Properties.md).

## Kinds of projects

| `StellarSdkUsage` | What it is | Gets |
|---|---|---|
| `ExecutableModule` | A game or an application | A generated launcher, `Exe` in Debug and `WinExe` otherwise (`StellarOutputType`) |
| `Module` | A custom library (level 5) | |
| `EngineModule` | An engine module of level 3 or 4 | The engine version, XML documentation |
| `EngineCoreModule` | A core system of level 2 | The engine version, XML documentation |
| `Tests` | A test project | xUnit and FluentAssertions (`StellarTestFrameworks=false` to bring others), no `.stellar.project` |
| `EngineCore` | `Stellar.Core` itself | The Kernel and the native wrapper only, no `.stellar.project` |

`Stellar.Kernel` and `Stellar.Core` are referenced implicitly by all of them, `EngineCore` excepted
(`StellarImplicitKernelReference`, `StellarImplicitCoreReference`). `.stellar.project` is required by games only: a
library without one has no assets, resources and runtime configuration.

## The wrapper of the native core

`Stellar.Core.Identifier`, `Label`, the registries and the other classes over the native core come from one of two
assemblies with the same classes (see [Native core](NativeAbi.md)). `StellarNativeWrapper` selects it:

| Value | Default | Reference with sources |
|---|---|---|
| `Cli` | On Windows, once the C++/CLI wrapper is built in the installation | The built `Stellar.Native.Core.Wrapper.Cli.dll`, x64, with `Stellar.Native.Core.dll` and `Ijwhost.dll` |
| `Managed` | Elsewhere, on Windows without C++/CLI support, and with `PublishAot` | `ProjectReference` to `Stellar.Native.Core.Wrapper.Managed` |

With sources every project references the wrapper, since its classes are part of the API of `Stellar.Core`. With
packages the wrapper comes inside the `Stellar.Core` package, which `scripts/build.ps1 -Pack` makes on the machine
that uses it, with the wrapper chosen there. The SDK stamps the choice into every assembly as
`[AssemblyMetadata("StellarNativeWrapper", ...)]`.

## Inside the core

The projects of the core are built with the SDK straight from its sources, no package. Such a project has no `Sdk`
attribute, so nothing defines `$(StellarRoot)` before its first line: it finds `Stellar.Sdk.InRepo.props` at the root
by name, and from there every path is `$(StellarRoot)`, whatever the depth of the project:

```xml
<Project>
    <Import Project="$([MSBuild]::GetPathOfFileAbove('Stellar.Sdk.InRepo.props'))"/>
    <PropertyGroup>
        <StellarSdkUsage>EngineCore</StellarSdkUsage>
    </PropertyGroup>
    <Import Project="$(StellarRoot)SDK/src/MSBuild/Stellar.Sdk/InRepo/Sdk.targets"/>
</Project>
```

Every other project of the core gets `$(StellarRoot)` from `Directory.Build.props`, and references with it:
`$(StellarRoot)Kernel/src/Managed/Stellar.Kernel/Stellar.Kernel.csproj`, never a relative path.

`InRepo` always links sources, puts the output under `artifacts/bin/<project>-<wrapper>/` so both wrappers build side
by side, and ignores `config/` of the installation: the core builds the same everywhere. It needs no task assembly:
the tasks only run for projects with a `.stellar.project`.

## Sources or packages

`StellarPrecompiled` selects how a module is linked: `ProjectReference` to the sources, or `PackageReference` to the
local feed.

| | Debug | Other configurations |
|---|---|---|
| The engine side: modules and tools of the installation | sources | packages |
| A game or a library | packages | packages |
| A game whose solution says `"Engine": "Sources"` | sources | packages |

A game sees the engine as a dependency, as any package, and its solution has its own projects only. A solution that
works on the engine itself switches to sources in its `.stellar.solution`; `stellar solution sync` then puts the engine
projects into the solution folders `Engine` and `Modules`, because an IDE loads a referenced project only from the
solution.

Set the property to override the default, or pin one module either way for the whole installation with `Link` in the
[manifest](../Formats/EngineManifest.md). If sources are requested but the core is not there, as on the machine of a
player, the SDK warns and falls back to packages.

The version of a package is always exact, as `[0.8.0]`.

## How references are resolved

Everything happens at MSBuild evaluation time, with no task. Restore and the IDE see the real references.

1. The catalog is the set of `StellarModuleInfo` items: the modules of the core are built in, the rest comes
   from `manifests/.generated/stellar.modules.props`.
2. That file is generated from the module descriptors and the manifest by `stellar modules sync`, because MSBuild
   cannot read JSON at evaluation time. It also holds a property group per module, active only in the project of
   that module: this is how a module project learns its level, version and dependencies.
3. `StellarDependencies` and a `PackageReference` the catalog knows become `StellarDependency` items, which are joined
   with the catalog and become references, by the `Link` of the module or by `StellarPrecompiled`.
4. A `StellarDependency` the catalog does not know is a build error.
5. A module of the installation taken as a package without a package in `packages/` is a build error that says to run
   `stellar modules build --pack`, instead of a failed restore.
6. A module taken as sources and missing from the solution the IDE builds is a warning that says to run
   `stellar solution sync`: an IDE loads a referenced project only from the solution.

The engine packs its packages again under the same versions while it is developed. NuGet unpacks a version once, so
`build.ps1 -Pack`, `stellar install` and `stellar modules build --pack` take the copies unpacked before out of the
NuGet cache: the next restore of a game takes the new ones.

## The build pipeline

| Target | Task | Result | Switch (`=false`) |
|---|---|---|---|
| `StellarResolveProject` | `ResolveStellarProject` | Parses `.stellar.project`, selects assets | `StellarEnableAssets` for the assets |
| `StellarCompileResources` | `CompileStellarResources` | `resources/**/*.sres` in the output | `StellarEnableResourceCompilation` |
| `StellarGenerateRuntimeConfiguration` | `GenerateRuntimeConfiguration` | `{Project}.stellar.runtime.config.json` | `StellarEnableRuntimeConfiguration` |
| `StellarGenerateLauncher` | `GenerateStellarLauncher` | The `Main` of an executable module | `StellarEnableLauncher` |

Each target runs `$(<Target>DependsOn)` first. Steps of your own go into `config/Stellar.Sdk.targets` of the
installation, or into the project, with `BeforeTargets`/`AfterTargets`.

A task only translates between MSBuild and `Stellar.Tools`. Logic goes to Tools, where the CLI and the Editor can
reach it too.

## Why the files are split the way they are

`Sdk.props` is evaluated before the body of the project, `Sdk.targets` after it. So `Sdk.props` must not depend on
anything the project sets, `$(StellarSdkUsage)` included: there it is always empty. Everything that depends on the
project is in the `.targets` files, and the settings that `Microsoft.NET.Sdk` reads are imported before its targets.

| File | Evaluated | Content |
|---|---|---|
| `Sdk.props` | Before the project | Framework, platform, paths |
| `Stellar.Sdk.Environment.props` | Before the project | Versions, where the installation is |
| `config/Stellar.Sdk.props` | Before the project | The settings of the installation |
| `Stellar.Sdk.Defaults.props` | Before the project | Link mode, the wrapper (`Stellar.Sdk.Wrapper.props`), the local feed |
| `Stellar.Sdk.Catalog.targets` | After the project | Who the project is, which modules exist |
| `Stellar.Sdk.Module.targets` | After the project | What a module gets from its descriptor |
| `Stellar.Sdk.Usage.targets` | After the project | Settings per `StellarSdkUsage` |
| `Stellar.Sdk.References.targets` | After the project | `StellarDependency` items become references |
| `Stellar.Sdk.Native.targets` | After the project | The wrapper of the native core, native libraries |
| `Stellar.Sdk.Analysis.targets` | After the project | The analyzers and what they read |
| `Stellar.Sdk.Metadata.targets` | After the project | `[AssemblyMetadata]` of the assembly |
| `Stellar.Sdk.Validation.targets` | After the project | Mistakes in the project file |
| `Stellar.Sdk.Pipeline.targets` | After `Microsoft.NET.Sdk` | The engine build steps |
| `config/Stellar.Sdk.targets` | Last | The build steps of the installation |

Every target of the SDK is named `Stellar*`: a project must not define targets with that prefix, or it replaces
one of them (MSBuild keeps the last definition of a target).

## The launcher

An application is a library with an entry point class, not an executable: the runtime loads it like any other
module. The SDK makes it executable with a launcher next to it: `<name>.exe`, the apphost of .NET, starts
`<name>.Launcher.dll`, whose generated `Main` hands the name of the application to `Stellar.Runtime`. The launcher is
compiled on its own, it is not part of the application. `dotnet run` and the IDE start it.

| Property | Default | Meaning |
|---|---|---|
| `StellarEnableLauncher` | `true` | `false`: the application stays a plain library |
| `StellarLauncherName` | `$(AssemblyName)` | The name of the executable |
| `StellarLauncherAssemblyName` | `$(AssemblyName).Launcher` | The name of the launcher assembly |
| `StellarOutputType` | `Exe` in Debug, `WinExe` otherwise | A console window or none |

What happens next: [Runtime](Runtime.md).

## Analyzers

The analyzers ship inside the SDK: the entry point (STLR01xx), the architecture of the modules (STLR02xx), the
configuration files (STLR03xx). Every rule and every build check: [Diagnostics](../Reference/Diagnostics.md).
