# Stellar.Sdk: properties and items

- [README.md](./../../README.md)

Everything Stellar.Sdk does by itself, and how to change it. A property is set in the project, in
`config/Stellar.Sdk.props` of the installation (for every project of it), or on the command line (`-p:Name=Value`).
The project wins over `config/`, `config/` over the defaults.

## What the project is

| Property | Default | Meaning |
|---|---|---|
| `StellarSdkUsage` | from the level, for a module | `ExecutableModule`, `Module`, `EngineModule`, `EngineCoreModule`, `Tests` (`EngineCore` is Stellar.Core itself) |
| `StellarModuleId` | `$(AssemblyName)` | The module this project is, looked up in the catalog |
| `StellarProjectName` | `$(AssemblyName)` | The name of the `.stellar.project` file and of the package |
| `StellarProjectConfigurationFile` | `$(StellarProjectName).stellar.project` | The project configuration |
| `StellarRequireProjectFile` | `true` for `ExecutableModule` | Whether a missing `.stellar.project` is an error |
| `StellarOutputType` | `Exe` in Debug, `WinExe` otherwise | The executable of a game: a console window or none. The game itself is a library |
| `StellarGameIcon`, `StellarGameManifest` | `<name>.ico`, `<name>.manifest` | Taken by the executable when the files exist |
| `StellarLauncherName` | `$(AssemblyName)` | The name of the executable of a game |
| `StellarLauncherAssemblyName` | `$(AssemblyName).Launcher` | The name of the launcher assembly behind it |

## Where the installation is

| Property | Default | Meaning |
|---|---|---|
| `StellarRoot` | `STELLAR_ROOT`, a `manifests/` above, the user pointer | The root of the installation |
| `StellarPackagesDir` | `$(StellarRoot)packages/` | The local feed |
| `StellarArtifactsDir` | `$(StellarRoot)artifacts/` | Everything built: `bin/` and `obj/`, see [the layout](../Architecture/Repository.md) |
| `StellarUseArtifactsOutput` | `true` | `false`: the plain `bin/` and `obj/` next to the project |
| `StellarArtifactsName` | `$(MSBuildProjectName)` | The folder of the project in `artifacts/bin` and `artifacts/obj` |
| `StellarModuleCatalog` | `$(StellarRoot)manifests/.generated/stellar.modules.props` | The catalog of modules |
| `StellarUseModuleCatalog` | `true` | `false`: only the modules of the core exist |
| `StellarUseLocalFeed` | `true` | `false`: the local feed is not added to restore |
| `StellarImportCustomizations` | `true` | `false`: `config/` of the installation is not imported |
| `StellarCustomBeforeSdkProps` | `$(StellarRoot)config/Stellar.Sdk.props` | Imported before the project body |
| `StellarCustomAfterSdkTargets` | `$(StellarRoot)config/Stellar.Sdk.targets` | Imported after everything |

## Modules and references

| Property / item | Default | Meaning |
|---|---|---|
| `<StellarDependency Include="Id"/>` | the `Dependencies` of the descriptor | A module the project depends on |
| `StellarPrecompiled` | `false` in Debug, `true` otherwise | Sources (`ProjectReference`) or packages (`PackageReference`); `Link` of the manifest wins per module |
| `StellarImplicitKernelReference` | `true` | Reference `Stellar.Kernel` |
| `StellarImplicitCoreReference` | `true` | Reference `Stellar.Core` |
| `StellarImplicitRuntimeReference` | `true` for `ExecutableModule` | Reference `Stellar.Runtime`, which the launcher calls |
| `StellarModuleAutoUsage` | `true` | Take `StellarSdkUsage` from the level |
| `StellarModuleAutoVersion` | `true` | Take `Version` from the descriptor |
| `StellarModuleAutoDependencies` | `true` | Take `StellarDependency` items from the descriptor |
| `StellarModuleLevel`, `StellarModuleVersion`, `StellarModuleDependencies`, `StellarModuleDescription` | the catalog | What the catalog says about the module; set one to replace it |

Read-only, set by the SDK: `StellarIsModule`, `StellarModuleKnown`, `StellarModuleDirectory`,
`StellarModuleDescriptorFile`, `StellarIsCoreProject`, `StellarSdkInRepo`.

## The native side

| Property / item | Default | Meaning |
|---|---|---|
| `StellarNativeWrapper` | `Cli` on Windows, `Managed` elsewhere and with `PublishAot` | The wrapper of the native core |
| `StellarReferenceNativeWrapper` | `true` | Reference the wrapper (with sources) |
| `StellarNativeConfiguration` | `$(Configuration)` | Which native build to take from `artifacts/bin` |
| `<StellarNativeLibrary Include="path.dll"/>` | the DLLs of the native projects of a module | Copied next to the assembly, packed into `runtimes/win-x64/native` |
| `StellarModuleCopyNativeLibraries` | `true` | Add the DLLs of the native projects of the module |

## The build steps

| Property | Default | Meaning |
|---|---|---|
| `StellarEnableAssets` | `true` | Copy and embed the assets of `.stellar.project` |
| `StellarEnableResourceCompilation` | `true` | Compile the resources |
| `StellarEnableRuntimeConfiguration` | `true` | Generate the runtime configuration |
| `StellarEnableLauncher` | `true` | Generate the launcher of a game: the executable, `<name>.Launcher.dll` |
| `StellarRuntimeConfigType` | `External` | `External` (a file next to the game) or `Embedded` (a resource of the game). The assembly says which, the runtime needs nothing else |
| `StellarLocalizationIndexType` | `External` | The same for localization files |
| `StellarResourcesOutputDir` | `$(IntermediateOutputPath)stellar/resources/` | Where compiled resources go |
| `StellarResolveProjectDependsOn` | | Targets run before `StellarResolveProject` |
| `StellarCompileResourcesDependsOn` | | Targets run before `StellarCompileResources` |
| `StellarGenerateRuntimeConfigurationDependsOn` | | Targets run before `StellarGenerateRuntimeConfiguration` |
| `StellarGenerateLauncherDependsOn` | | Targets run before `StellarGenerateLauncher` |
| `StellarTasksAssembly` | the tasks of the SDK package | Another build of the tasks |

## Checks

| Property | Default | Meaning |
|---|---|---|
| `StellarAnalyzers` | `true` | The analyzers of the engine |
| `StellarAnalyzersAssembly` | the analyzers of the SDK package | Another build of the analyzers |
| `StellarAnalyzeConfiguration` | `true` | Give `.stellar.project` and `.stellar.module` to the analyzers |
| `StellarValidateProjectFile` | `true` | The STLRB checks of the project file |
| `StellarStampMetadata` | `true` | `[AssemblyMetadata]` with the kind, the wrapper, the module id, level and dependencies |
| `StellarTestFrameworks` | `true` | xUnit and FluentAssertions for `Tests` |

## Native projects

`Shared/Build/Stellar.Native.props` and `Stellar.Native.Module.props`, imported by `.vcxproj` files:

| Property | Default | Meaning |
|---|---|---|
| `StellarNativeBinDir` | `$(StellarRoot)artifacts\bin\` | Where binaries go: `<name>\<configuration>\`, as for managed projects |
| `StellarNativeObjDir` | `$(StellarRoot)artifacts\obj\` | Intermediate files |
| `PlatformToolset`, `WindowsTargetPlatformVersion`, `CharacterSet` | `v145`, `10.0.26100.0`, `Unicode` | Set before the import to change |
| `StellarNativeLinkCore` | `true` | Link `Stellar.Native.Core`; `false` for a module that only needs the Kernel headers |
| `StellarCustomNativeProps` | `$(StellarRoot)config\Stellar.Native.props` | The native settings of the installation |
