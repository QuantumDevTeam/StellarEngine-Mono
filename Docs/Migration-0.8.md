# Migrating modules to 0.8

- [README.md](./../README.md)

What changed for the code that uses the core: the modules of the monorepo that have not moved yet, and anything
built against the 0.7.0 packages of earlier states of this repository.

| Component | Monorepo | 0.7.0 state | Now | Why |
|---|---|---|---|---|
| Orchester, Kernel, Core, engine modules | 0.6.0 | 0.7.0 | 0.8.0 | Breaking: contexts and loops in the Kernel, engine instances in Core |
| Stellar.Runtime | | | 0.8.0 | New, released with the Kernel and Core it starts |
| Tools | 0.1.0 | 0.2.0 | 0.3.0 | Breaking: the launcher generator |
| Stellar.Sdk | 0.2.0 | 0.3.0 | 0.4.0 | Breaking: games are libraries behind a generated launcher |
| CLI | 0.1.0 | 0.2.0 | 0.2.1 | Update: same commands, the new Tools |
| P/Invoke exports | 1.0 | 1.0 | 2.0 | Every registry call takes a context |
| Runtime configuration format | 1 | 1 | 2 | Lists the modules to load: rebuild games |
| Native module contract | 1.0 | 1.0 | 1.0 | Unchanged |

## Project files

```xml
<!-- Before -->
<Project Sdk="Stellar.Sdk/0.2.0">
    ...
    <ProjectExtensions>
        <StellarDependency Include="Stellar.Logging" Type="EngineModule"/>
    </ProjectExtensions>
</Project>

<!-- After: the project of a module -->
<Project Sdk="Stellar.Sdk">
</Project>
```

```json
// After: Stellar.Graphic.stellar.module, next to src/Managed/Stellar.Graphic/
{
  "Id": "Stellar.Graphic",
  "Level": "HighLevel",
  "Dependencies": ["Stellar.Logging"]
}
```

- A module is a folder with a [`.stellar.module`](Formats/ModuleDescriptor.md): its kind, version, description and
  dependencies come from there, and the project file is nearly empty. See [Modules](Architecture/Modules.md).
  `stellar modules new` creates the folder, the descriptor, the projects and the tests.
- The SDK version moved to `global.json`.
- Dependencies are plain `StellarDependency` items: the descriptor makes them, a project may add or remove some.
- `StellarUsageDefinations.props` and `WorkloadSetup.targets` are gone. `StellarOutputType` is back, as the override of
  `Exe`/`WinExe` for games.
- `$(StellarOrchesterInstallationDir)` is now `$(StellarRoot)`: the root of the installation, which is the core itself.
- `STELLAR_HOME` is now `STELLAR_ROOT`, and `$STELLAR_HOME/engine-core` is gone: the core is the root.

## Kernel

| Before | After |
|---|---|
| `Stellar.Kernel.Label.ILabel`, `ILabeled` | `Stellar.Kernel.ILabel`, `ILabeled` |
| `FastConcurrentIdentifierMap`, `ConcurrentIdentifierMap` in Kernel | In `Stellar.Core.Data.Collections` |
| `IContext.Data` | `IContext<TData>.Data`, by reference |
| `IContextData` on anything | Still a structure, now enforced by the constraint of `IContext<TData>` |
| `StellarEntryPoint.Run(IContext)` | `Run<TContext>(ref readonly TContext)` |
| `StellarEntryPoint.RequestStop(IContext)` | `RequestStop<TContext>(ref readonly TContext)` |
| `IFailureDispatcher.Dispatch(IFailureContext)` | `Dispatch<TContext>(ref readonly TContext)` |
| `IFailureHandler.Handle(IFailureContext)` | `Handle<TContext>(ref readonly TContext)` |
| `IFailureHandlerProvider.GetHandlers(IFailureContext)` | `GetHandlers(IFailure)` |
| `IFailureCatch`: `TryAction`, overloads with `Func` | `Try`, `Except`, `Finally`, `Handle`, plus `Caught` |
| `IQuant.Meta` | Also `Metas` and `FindMeta<TMeta>()` |
| `Stellar.Core.Wrapper.CLI` stub | Two wrappers of the native core with the same classes, picked by the SDK, see [Native core](Architecture/NativeAbi.md) |
| `ConfigurationComponent` | New abstract `Kind` |
| `IRunContext`: `Logger`, `FailureDispatcher` | Also `Services` and `Engine`: the engine the module runs in |
| `IFrameContext`: `Phase`, `FrameIndex`, `DeltaTime` | Also `Clock`, `Time`, `UnscaledDeltaTime`, `Loop`. `DeltaTime` is the step of the clock of the participant |
| `FramePhase` | `FixedUpdate` between `PreUpdate` and `Update`, for `IFixedUpdatable` |
| | New: `IEngineInstance`, `IMainLoop`, `IMainLoops`, `IMainThreadDispatcher`, `Stellar.Kernel.Timing` (`IClock`, `ITimeSystem`, `ITimer`, `ITimeSource`), `IFailureReporter`, `IServiceHost`, `StellarRuntimeConfigurationAttribute`, `ModulesComponent` |

The stack-only contexts could not be passed to the old signatures at all: a `ref struct` does not convert to an
interface. This is why contexts are now generic with `allows ref struct`.

`EntryPoint`, `EventSystem`, `Failures`, `JobSystem`, `Lifecycle`, `Threading` and `Timing` are compiled for `net9.0` only, since they need
`allows ref struct`. The `netstandard2.0` target is for build-time clients.

## Core

| Before | After |
|---|---|
| `Stellar.Core.Label.Label` | `Stellar.Core.Label` |
| `Stellar.Core.Global.Engine.Logger`, `FailureDispatcher` with internal setters | `Engine.Services`, modules publish there |
| `Stellar.Core.Global.Engine`: state of the process | A view of `CoreEngine.Current`: one `CoreEngine` per engine instance, chosen by `EngineScope`. `CoreEngine.Default` outside any engine |
| `IdentifierRegistry.Instance`, `LabelRegistry.Instance`, `ContainerRegistry.Instance`, `ModuleRegistry.Instance` | `CoreContext.Current.Identifiers`, `.Labels`, `.Containers`, `.Modules`, or the context of an engine: `CoreContext.For(engine)` |
| `DataContainerRegistry.Instance`, `QuantsRegistry<T>.Instance`, `MetaQuantsRegistry<T>.Instance` | `CoreEngine.Current.Registry.DataContainers`, `.Quants<T>()`, `.MetaQuants<T>()` |
| P/Invoke exports 1.0 | 2.0: every registry call takes a `StellarCoreContext` first, null for the default context |
| `Engine.Registry.IdentifierRegistry`, ... | `Engine.Registry.Identifiers`, `Labels`, `DataContainers`, `Quants<T>()`, `MetaQuants<T>()` |
| `Engine_Entries` | Removed, this is the job of the runtime |
| `Stellar.Core.EntryPoints.EntryPointHost` | `Stellar.Runtime`: the launcher calls `StellarRuntime.Run`, see [Runtime](Architecture/Runtime.md) |
| `Stellar.Core.Configuration.RuntimeConfigurationReader` | `Stellar.Runtime.Configuration.RuntimeConfigurationReader` |
| `EntryPoint` of the Core module | Also `IServiceHost`, and publishes an `IFailureReporter` |
| `RunContext`, `StopContext` in `Stellar.Core` | In `Stellar.Core.EntryPoints` |
| `FailureContext.GetData<T>()` and the like | `Stellar.Core.Data.Context.Context<TData>` |
| `HandlerProviderMeta` with a tuple | `FailureBinding`, and `RegisterDefaultHandler` for a catch-all |
| `Quant.Meta { get; init; }` | `MetaQuant` is set by the constructor, `Attach` and `Detach` add more |
| `TimedMetaQuant.SetLifetimeAsync` and friends | Removed, they did nothing asynchronous |
| `Extensions.TraceModules` | Removed |
| `Stellar.Core.Identifier`, `Label` and their registries in C# | Native: the C# classes come from the wrapper of the native core |
| `Stellar.Core.Interop.NativeCore`, `INativeCoreBackend`, `STELLAR_NATIVE_BACKEND` | `Stellar.Core.Native.NativeCore`; the SDK picks the wrapper with `StellarNativeWrapper` |
| Membership of `DataContainer` and branches of `Roster` in C# dictionaries | `ContainerRegistry` of the native core, the values stay in C# |
| `Label.Create(name, identifier = null, register = true)` | Overloads, the flag is `shouldRegister` |
| `Stellar.Core` built with the plain .NET SDK | Built with `Stellar.Sdk` from its sources, `StellarSdkUsage=EngineCore` |

Behavior changes worth checking:

- `Register` and `Unregister` now throw `ArgumentException` for a registry of the wrong type. Before, the call was
  silently ignored. `Identifier.Register` and `Label.Unregister` hit this even with their own registry, so
  `Identifier.CreateAndRegister` registered nothing and disposing a label did not free its name.
- `Label.Create` returns `null` when the name is taken, instead of an unregistered label.
- `IFailureHandler.Handle` returns `true` to stop execution, as its documentation always said. The old code had it inverted.
- `Identifier` and `Label` are values: a registry hands out an equal object, not the same one. Compare with `Equals`,
  not by reference.
- A `DataContainer` must be disposed: that destroys its native membership.

## Games

A game is now a library loaded by the runtime, like a module. The SDK puts a launcher next to it: `<name>.exe` and
`<name>.Launcher.dll`. Nothing changes in the project file; what changes:

- the entry point of a game is started once the engine is up, and its `Run` is the start of the game, not its
  whole life: `Run` returns, and the main loop runs the frames. A game that is `IUpdatable` or `IRenderable` takes
  part in them. A game that did everything in `Run` sets `"MainLoop": false` in the `Runtime` section;
- the engine stops when asked to: `IEngineControl.RequestStop`, from `context.Services`, or Ctrl+C;
- `StellarRuntimeConfigType=Embedded` works: the assembly says where its configuration is;
- `ApplicationIcon` and `ApplicationManifest` of a game go to the executable, through `StellarGameIcon` and
  `StellarGameManifest`;
- a configuration built by an older SDK has no `Modules` component: rebuild the game.

## Native

Include paths start at the root of the module, and consumers go through `include/Core/core.h` and
`include/Core/core_contracts.h`.

| Before | After |
|---|---|
| `Stellar.Native.Core` static library | Shared library: registries must exist once per process |
| `_Mixins/`, `Extensions.h`, `STELLAR_*` body macros | `Mixins/`, CRTP with no virtual functions |
| Raw `T*` and `T&` in the API | `Ptr<T>` and `Link<T>` of the Kernel |
| C ABI functions inside `Stellar.Native.Core`, `include/Core/abi.h` | `Stellar.Native.Core.Wrapper.PInvoke`, `include/PInvoke/exports.h` |
| Collections return generators | They return `std::vector`: the public headers are C++20 |
| `ConcurrentFlatMap` | Removed, it was marked deprecated as broken |
| `module/*.ixx` | Removed, they referred to files that do not exist |
| `ConcurrentPriorityQueue::PopWait` returns `T` | Returns `std::optional<T>`, empty when cancelled |
| `Identifier::FromString` | `Identifier::Parse`, returns `std::optional` |
| `DoubleBufferedSnapshotMap::GetCurrentSnapshot` | `GetSnapshot` and `TakeSnapshot` |
| `Label` constructed over temporary text | `LabelRegistry::Register` copies the name |
| `LabelRegistry::Register(label)` | `Register(name, id)` |
| `IdentifierRegistry::Unregister(id, out)` | `Unregister(id)` |

## Repository and installation

| Before | After |
|---|---|
| `$STELLAR_HOME` with `engine-core/` inside | `$STELLAR_ROOT`: a checkout of StellarEngine-Core is the root |
| `src/Kernel`, `src/Core`, `src/SDK`, `src/Tools`, `tests/` | `Kernel/src/{Managed,Native}`, `Core/src/{Managed,Native,NativeInterop}`, `SDK/src/{MSBuild,Roslyn,...}`, `*/tests` |
| `Data/version.props`, `Data/Templates`, `Data/Schemas` | `Shared/Build/Stellar.Versions.props`, `Shared/Templates`, `Shared/Schemas` |
| `src/Native.Common.props` | `Shared/Build/Stellar.Native.props`, and `Stellar.Native.Module.props` for modules |
| `StellarHome` (Tools) | `StellarInstallation` |
| Manifest format 1: every module listed with its fields | Format 2: discovery of `modules/`, and entries only for what an installation changes |
| `Projects/Module` template | `Projects/Library`; engine modules come from `Modules/Managed` and `Modules/Native` |
