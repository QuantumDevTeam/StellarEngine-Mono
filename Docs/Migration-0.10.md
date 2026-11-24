# Migrating to 0.10

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

What changed for the code that uses the core, from 0.9. The release is small: the failure system got a name of its
own, the logger got its contracts, and the SDK learned the modules of a game.

| Component | 0.9 | 0.10 | Why |
|---|---|---|---|
| Orchester, Kernel, Core, Runtime | 0.9.0 | 0.10.0 | Breaking: the namespace of the failure system, and `ILogger` |
| Stellar.Sdk | 0.4.0 | 0.4.1 | Compatible: the modules of a solution |
| Stellar.Tools | 0.3.1 | 0.3.2 | Compatible |
| `stellar` (CLI) | 0.2.2 | 0.2.3 | Compatible |
| P/Invoke exports | 3.1 | 3.1 | Nothing changed in the native part |

Every component has a version of its own: a breaking change raises the second number of that component, and only of
the components it breaks. The orchestra takes the second number when any of its components does.

## Where `global.json` points

A project pins `Stellar.Sdk` in its `global.json`, and the local feed holds the version of the installation only, so
after installing 0.10 every pin of `0.4.0` fails to restore. Change it to `0.4.1`:

```json
{ "msbuild-sdks": { "Stellar.Sdk": "0.4.1" } }
```

`stellar project new` and `stellar modules new` write the current one. Then build the engine again and install it
(`scripts/build.ps1 -Configuration Release -Install`), so that the packages of the installation are the new ones.

## Failures

The types of the failure system moved from `Stellar.Kernel.Failures` to `Stellar.Kernel.FailureSystem`, named after
the system as `Stellar.Kernel.LoggingSystem` is. Nothing else about them changed.

| Before | Now |
|---|---|
| `using Stellar.Kernel.Failures;` | `using Stellar.Kernel.FailureSystem;` |
| `Stellar.Kernel.Failures.Catcher`, `Failure`, `FailureException`, `FailureKind`, `FailureLevel`, `FailureReaction`, `IFailureSystem` | the same names in `Stellar.Kernel.FailureSystem` |

The public members of Stellar.Core and Stellar.Runtime that mention them (`Engine.Failures`, `IRunContext.Failures`,
`IEngineControl.RequestStop(reason, Failure)`, `RuntimeCatchers`) keep their names: only the types they use moved, so a
module needs the new `using` and a rebuild. A module built against 0.9 does not load against 0.10.

## Logging

`Stellar.Kernel.LoggingSystem` has its contracts. The logging itself is a module; the Kernel says what it implements.

| Before | Now |
|---|---|
| `ILogger : IRegistrableQuant` | `ILogger : IQuant`: a logger is found by its name, in a catalog, and is not put into a registry |
| `ILogger`: `Log`, `LogWithoutFormat` | the same, and `Start()`, `Stop()`: a logger can be switched on and off |
| nothing | `ILoggerCatalog : ICatalog<string, ILogger>`: the loggers of an engine by name, with the members of any catalog |
| nothing | `LoggerExtensions`: `Info`, `Debug`, `Success`, `Warning`, `Error`, `Exception(message, exception)`, `Separator` |
| `LogLevel`: `Info`, `Debug`, `Success`, `Warning`, `Error`, `Exception` | in the order of severity: `Debug` is 0 and `Info` is 1; the rest keep their values. Rebuild what stores or compares the numbers |
| nothing | `LogFormat(Format, ColorizedFormat)`, and `LoggerFormat`: one `LogFormat` per level (`ForLevel`), with `LoggerFormat.Default` |

A logger of a module is added to the catalog when the module starts and taken out when it stops:

```csharp
public override int Run<TContext>(ref readonly TContext context)
{
    _catalog = context.Services?.Find<ILoggerCatalog>();
    _catalog?.TryAdd(Name, _logger);
    return 0;
}

public override void RequestStop<TContext>(ref readonly TContext context) => _catalog?.Remove(Name);
```

The catalog is published by the logging module, which also publishes the `ILogger` that writes into every logger of the
catalog. Without one `Find<ILoggerCatalog>()` is `null`. A class that implemented `ILogger` implements
`Start` and `Stop` now.

## Modules of a solution

A game keeps its own libraries as modules, and the game names them in `StellarDependencies` like any module of the
engine: a folder with `<Id>.stellar.module` (`"Level": "EndPoint"`), found below the solution or listed in `Modules` of
`.stellar.solution`. `stellar solution sync` writes them into `.stellar/stellar.modules.props`, which the SDK imports
after the catalog of the installation. They are always taken as sources, and are loaded by the runtime in a context of
their own after their dependencies. See [`.stellar.solution`](Formats/SolutionFile.md#modules-of-a-solution).

| Before | Now |
|---|---|
| a library of a game as a plain `ProjectReference`: it compiles, but the runtime never loads it as a module | a module of the solution, named in `StellarDependencies` |
| `Unknown Stellar modules: X` for a library beside the game | the same message, which now also says to run `stellar solution sync` |

The rest, for those who work on the tools:

- `StellarSolutionModuleCatalog` (the property) is found from any project below the solution; `StellarUseSolutionModuleCatalog=false`
  turns it off.
- The catalog gives every module two properties, `StellarModuleCatalogFile` and `StellarModuleSyncCommand`, so that
  STLRB102 checks a descriptor against the catalog it is in and names the command that writes it.
- `ModuleSource.Solution`, `ModuleDiscovery.Discover(directories, depth)`, `ModuleCatalogGenerator.Render(modules, origin)`
  and `SolutionModules` in `Stellar.Tools`. The search for modules skips `bin` and `obj`.
