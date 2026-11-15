# `.stellar.module`

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

The identity of a module in the engine: the file `<Id>.stellar.module` at the root of the module folder. The schema
is `Shared/Schemas/stellar.module.schema.json`, published on the site of the core, and `stellar modules new` writes the
`$schema` line with that address (`StellarSchemasUri`), so the IDE checks and completes the file on any machine.

```json
{
  "$schema": "https://quantumdevteam.github.io/StellarEngine-Core/schemas/stellar.module.schema.json",
  "Id": "Stellar.Logging",
  "Version": "0.8.0",
  "Level": "CoreModule",
  "Description": "Logging for the engine and its modules",
  "Dependencies": ["Stellar.FileSystem"]
}
```

Only `Id` is required. A module needs exactly as much description as it differs from the conventions:

| Field | Default | Meaning |
|---|---|---|
| `Id` | | The identifier, also the assembly and the package name. The file must be named after it |
| `Version` | the engine version | The version of the module and of its package |
| `Level` | `HighLevel` | `CoreModule`, `Runtime`, `HighLevel` or `EndPoint`, see [levels](../Architecture/Core.md) |
| `Description` | | One sentence. It becomes the package description |
| `Project` | `src/Managed/<Id>/<Id>.csproj`, `src/<Id>/<Id>.csproj` or `<Id>.csproj` | The managed project. `""`: the module has none |
| `NativeProjects` | every `.vcxproj` under `src/Native`, then `src/NativeInterop` | The native projects, in build order |
| `TestProjects` | every `.csproj` and `.vcxproj` under `tests` | The test projects |
| `Repository` | | Where the sources come from. It becomes the package project URL |
| `Dependencies` | `[]` | The modules this one depends on. `Stellar.Kernel` and `Stellar.Core` are implicit |

Paths are relative to the module folder. An entry of the [manifest](EngineManifest.md) may replace any field for one
installation.

## The folder of a module

```
Stellar.Logging/
├── Stellar.Logging.stellar.module
├── src/Managed/Stellar.Logging/     Stellar.Logging.csproj
├── src/Native/...                   native projects
├── src/NativeInterop/...            wrappers of the native part
└── tests/...                        test projects
```

The same layout as the core. `stellar modules new` creates it from `Shared/Templates/Modules`.
