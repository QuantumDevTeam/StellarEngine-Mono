# Migrating to 0.9

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

What changed for the code that uses the core, from 0.8.

| Component | 0.8 | 0.9 | Why |
|---|---|---|---|
| Orchester, Kernel, Core, Runtime | 0.8.0 | 0.9.0 | Breaking: identifiers and labels are values; the contracts of the file system |
| P/Invoke exports | 2.0 | 3.0 | The registries of identifiers and labels are gone |

## Identifiers

An identifier is a value of the Kernel, not a class of a wrapper, and it is never registered.

| Before | Now |
|---|---|
| `Stellar.Kernel.IIdentifier` | `Stellar.Kernel.Identifier`, a struct |
| `Stellar.Core.Identifier` (from a wrapper) | `Stellar.Kernel.Identifier` |
| `Identifier.Create()`, `Identifier.CreateAndRegister()` | `Identifier.New()` |
| `identifier.UID` (a `Guid`) | the identifier itself: compare it, write it (`ToString`, `ToBytes`), read it (`Parse`, `FromBytes`) |
| `Identifier.Parse(text)` returning `null` | `Identifier.Parse(text)` throws, `Identifier.TryParse(text, out id)` |
| `ToCanonicalString()` | `ToString()`, the same text as the native core |
| `identifier.Register()`, `Dispose()`, `IsRegistered`, `Identifier.Get(...)` | nothing: objects are registered, not identifiers |
| `CoreContext.Identifiers`, `Engine.Registry.Identifiers` | nothing |
| `null` for no identifier | `Identifier.Empty`, `IsEmpty` |

`Guid` is not part of the API of the engine any more: code works with identifiers.

## Labels

A label is an identifier and a name, a value. There is no registry of labels and no lookup by name.

| Before | Now |
|---|---|
| `Stellar.Kernel.ILabel`, `Stellar.Core.Label` | `Stellar.Kernel.Label`, a struct |
| `Label.Create(name)`, registered | `Label.New(name)`, or `new Label(identifier, name)` |
| `Label.Get(name)`, `Label.Get(identifier)` | nothing: keep the label |
| `string` converted to a `Label` implicitly | nothing: make the label |
| `CoreContext.Labels`, `Engine.Registry.Labels` | nothing |
| `IEventType : ILabel` | `IEventType : IMetaQuant, ILabeled` |

## Registries

Objects no longer register themselves: registries do, and tell them.

| Before | Now |
|---|---|
| `IRegistrableQuantumObject.Register(registry)`, `Unregister(registry)` | `registry.Register(item)`, `registry.Pop(id)` or `Unregister(id)`; the object lists its registries in `Registrations` |
| `IRegistrableQuantumObject : IQuantumObject` | `: IIdentifiableQuantumObject`, with `Registrations` |
| `CoreEngine.Registry.Quants<T>()`, `MetaQuants<T>()` | `CoreEngine.Registry.Of<T>()`, for any registrable type, bases and contracts too |
| `QuantsRegistry<T>`, `MetaQuantsRegistry<T>`, `DataContainerRegistry`, `ConcurrentRegistry<T>` | `Registry<T>` |
| a cast after `registry.Get(id)` | `registry.Get<TDerived>(id)`: `null` when the object is not one |
| a quant in the registry of its exact type | in the registry of its home type and of every type of its hierarchy marked `[StellarRegistry]` |

`RegistersImplicitly` turns the implicit registration off for a class.

## Collections and containers

Containers hold the objects of the world, collections hold working data. Both are managed only: the native
mirror of the membership of containers is gone (`ContainerRegistry`, `stellar_container_*`, `CoreContext.Containers`).

| Before | Now |
|---|---|
| `DataContainer<T>` | `DataContainer<TKey, TValue>`: items by key, registered, sealable, `Changed` |
| `ConstantTable<T>`, `WritableTable<T>` | `Table<T>`: objects by identifier, `Get<TItem>`; `Seal()` instead of a read-only type |
| `Roster<T>` with `SaveBranch`, `PopBranch` | `Roster<T>`: `AddBranch`, `NewBranch`, `RemoveBranch`, `Find` through the branches; never holds itself |
| nothing | `DataTable<TRow>`: rows of one type by name |
| `IDataContainer.Get(id)`, `ContainsKey`, `Contains(obj)` | `IDataContainer` (`Count`, `IsSealed`) and `IDataContainer<TKey, TValue>` (`TryGet`, `Get`, `ContainsKey`, `Keys`, `Values`) |
| `DataContainer<T>.GetContainer(id)` | `Engine.Registry.DataContainers.Get<Table<T>>(id)` |
| `FastConcurrentIdentifierMap<T>`, `ConcurrentIdentifierMap<T>`, `IdentifierComparer` | `ConcurrentMap<TKey, TValue>` |
| nothing | `ConcurrentList<T>`, `ConcurrentSet<T>` |

`ConcurrentList`, `ConcurrentMap` and `ConcurrentSet` are safe from any thread, read without a lock, enumerate a
snapshot shared until the next change, and raise `Changed` with what changed. `Catalog<TKey, TValue>` is a
`ConcurrentMap` that is an engine catalog.

`GetEnumerator()` of the collections, the containers and `Registrations` gives a `SnapshotEnumerator<T>`, a struct:
`foreach` allocates nothing. `Keys`, `Values` and `Snapshot` of a map, and `Keys` and `Values` of a registry, are
arrays shared until the next change: do not change them.

## Catalogs

What is found by a key of its own, not by an identifier, is a catalog: `ICatalog<TKey, TValue>` (`Set`, `TryAdd`,
`TryGet`, `Get`, `Contains`, `Remove`), and `IReadOnlyCatalog<TKey, TValue>` for what can only be read.
`Stellar.Core.Data.Catalog.Catalog<TKey, TValue>` implements it, with hooks for the form of keys, the values it
refuses and what a missing key throws. A registry is only what holds objects by their identifiers.

| Before | Now |
|---|---|
| `IServiceRegistry`, `ServiceRegistry` | `IServiceCatalog`, `ServiceCatalog`: a catalog of `Type` to `object`, refusing a service that does not fulfil its contract |
| `services.Set<T>()`, `Find<T>()`, `Get<T>()`, `Remove<T>()` | the same, extensions in `Stellar.Kernel.Services` (`using Stellar.Kernel.Services;`) |
| `IContentTypeRegistry.Register(extension, type)` | `IContentTypeCatalog.Set(extension, type)` |
| `IFileSystem.RegisterDriver(driver)`, `TryGetDriver(scheme, out driver)` | `fileSystem.AddDriver(driver)`, `fileSystem.Drivers.TryGet(scheme, out driver)` |
| `IFileSystem.GetDomain(name)`, `TryGetDomain(name, out domain)`, `Domains` as a collection | `fileSystem.Domains.Get(name)`, `TryGet(name, out domain)`: a read-only catalog |

## Failures

One failure system per engine and one catcher everywhere, the runtime included.

| Before | Now |
|---|---|
| `IFailureLevel`, the classes `NonCritical`, `Warning`, `Error`, `CriticalError` | `FailureLevel`, an enum with the same names, ordered |
| `IsLoggable`, `IsStopExecute`, `ShouldTerminate` of a level | `FailureReaction`: `Log`, `StopExecution`, `StopEngine`, from the catalogs of `IFailureSystem` |
| `FailureType`, a closed enum of flags | `FailureKind`, an open set (`new FailureKind("audio")`); the phase of a frame is `Failure.Phase` |
| `IFailure`, the struct `Failure` | `Failure`, a class: kind, level, message, exception, sender, phase, reaction |
| `IFailureDispatcher`, `IFailureReporter`, handler providers and bindings | `IFailureSystem`: `Report`, `ReactionTo`, `AddHandler(handler, filter)` |
| `IFailureHandler.Handle(ref TContext)` | `IFailureHandler.Handle(Failure)`: it hears; the reaction is decided by the system |
| `IFailureCatch`, `FailureCatch` (`Try`/`Except`/`Finally`/`Handle`) | `Catcher`: `using`, `Catch<T>` blocks, `Run`, `Try`, `RunAsync`, `TryAsync`, `Guard`, `Report` |
| `RuntimeCatcher`, `CatchResult` | `RuntimeCatchers` of plain `Catcher`s, and `RuntimeCatchers.MustStop(level, failure)` |
| `Engine.FailureLevels`, `Engine.FailureDispatcher`, `CoreEngine.FailureLevels` | `Engine.Failures`, `CoreEngine.Failures` |
| `IRunContext.FailureDispatcher` | `IRunContext.Failures` |
| `IEngineControl.RequestStop(reason, IFailure)` | `RequestStop(reason, Failure)` |
| `DomainNotFoundException : IOException` | `DomainNotFoundException : FailureException`, of the file system |

A reaction that stops the work makes a catcher throw the failure on, as if the call had not been caught.

A catcher keeps every failure it caught by default. `Keeps` sets how many of the last ones `Failures` keeps, and
`FailureCount` counts them all; the catchers of the runtime keep 16, so a failure that repeats on every frame costs no
memory.

## Phases of the main loop

| Before | Now |
|---|---|
| `void Update<TContext>(ref readonly TContext context) where TContext : IFrameContext, allows ref struct`, the same for every phase | `void Update(in FrameContext context)` |
| `Stellar.Runtime.Loop.FrameContext` | `Stellar.Kernel.Lifecycle.FrameContext`, the one context of a phase |

A phase is a plain interface call: half the cost of the generic one, for every participant of every frame.
A participant is not called for a phase it only has the empty default of.

## Native memory

New: `NativeAllocator` (one per engine, `CoreContext.Allocator`, and any made apart), `NativeList<T>` with one owner and
`ConcurrentNativeList<T>` for many threads, in `Stellar.Core.Memory`, over `Abi/Memory.h`; `NativeListView<T>` for
native code, in `include/Kernel/memory.h`. The P/Invoke exports are 3.1.

## Profiling

New, in `Stellar.Kernel.Profiling`: markers, counters and categories for code; `IProfilerSession`, which a profiler
module implements and attaches to an engine (`Profiler.Attach`); `ProfilerEvent`, shared with native code
(`Abi/Profiling.h`). The runtime marks frames, phases, timers, posted work and stages, and, on demand, every call of a
participant.

## Solutions and `stellar run`

`stellar project new` makes a solution: `Name.sln`, `Name.stellar.solution`, `global.json` and `NuGet.config` at the
root, the project in `Name/`; the engine sources it is built against in Debug are in the solution folders `Engine` and
`Modules`. `--flat` keeps the old layout. `stellar run` builds and runs from anywhere in the solution, by the profiles of
`.stellar.solution`. The templates keep their project in a folder `__ProjectName__/`.

A game or a library takes the engine from its packages in every configuration, and keeps `bin/` and `obj/` next to
itself: the IDE shows the engine as a dependency and the project as its own files. `"Engine": "Sources"` in the
`.stellar.solution`, then `stellar solution sync`, brings back the sources in Debug. Modules and tools of the
installation are unchanged: sources in Debug, `artifacts/`.

A game lists its modules in a property, `<StellarDependencies>Stellar.FileSystem</StellarDependencies>`, or declares one
as a package, `<PackageReference Include="Stellar.FileSystem"/>`, which its NuGet window can add.
`<StellarDependency Include="..."/>` still works, but Rider shows it as a missing file of the project. Packing again under the same version takes the old copy out of the NuGet cache.

In the catalog of modules, how a module is linked is the metadata `LinkMode`, no longer `Link`, which IDEs read as the
display path of a file: the references to the engine showed up as files of the project. Run `stellar modules sync` to
write the catalog again; an old one is still read.

## Code outside any engine

`EngineScope.Resolve()` is what Stellar.Core and the wrappers use to find the current engine. Outside any engine it
gives the default instance of the process (`DetachedPolicy.Default`, tools and tests), or throws
(`DetachedPolicy.Throw`), which `StellarRuntime.Run` sets for a running application.

## The file system

The contracts of `Stellar.Kernel.FileSystem` are new: `FilePath`, `DomainScheme`, `IFileDriver`, `IDomain`,
`IFileSystem`, and the resources (`IResource`, `IResourceSystem`). `DomainType`, `IPath`, `IFile`, `IFileType`,
`IFileProvider`, `IFileProviderFactory`, `IResourceFactory` and `IResourceOrchestrator` are gone. They are compiled for
`net9.0` only. The module Stellar.FileSystem implements them.
