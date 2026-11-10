# `engine.manifest.json`

- [README.md](./../../README.md)

How an installation finds its modules, and what it changes in them: `manifests/engine.manifest.json`, created by
`stellar install`. The schema is `Shared/Schemas/engine.manifest.schema.json`.

The modules describe themselves, in their [`.stellar.module`](ModuleDescriptor.md) files, and are found
automatically. The manifest only holds what this installation does differently, so a fresh one is nearly empty:

```json
{
  "SchemaVersion": 2,
  "OrchesterVersion": "0.8.0",
  "Discovery": {
    "Enabled": true,
    "Directories": ["modules"],
    "Depth": 2
  },
  "Modules": [
    { "Id": "Stellar.Logging", "Path": "F:/project/CS/Stellar.Logging" },
    { "Id": "Stellar.Physics", "Enabled": false },
    { "Id": "Stellar.Network", "Link": "Package" },
    { "Id": "Stellar.Audio", "Version": "0.8.0", "Level": "HighLevel" }
  ]
}
```

## Discovery

| Field | Default | Meaning |
|---|---|---|
| `Enabled` | `true` | Whether modules are found automatically. Without it, only the modules of `Path` entries exist |
| `Directories` | `["modules"]` | Folders searched, relative to the root or absolute |
| `Depth` | `2` | 1 is `modules/<module>`, 2 also `modules/<group>/<module>` |

A folder with a `<Id>.stellar.module` file is a module, and the search does not go inside it. Folders whose name
starts with `.` are skipped.

## Module entries

One entry per module. Every field but `Id` is optional: what is set replaces the value of the descriptor, what is
not keeps it.

| Field | Meaning |
|---|---|
| `Id` | The module |
| `Enabled` | `false` turns the module off without removing its folder |
| `Path` | The module folder, absolute or relative to the root. It wins over a discovered folder of the same module: this is how a module is worked on in its own repository |
| `Link` | `Sources` or `Package`: how every project references the module, whatever the configuration. `Default` follows `StellarPrecompiled` |
| `Version`, `Level`, `Description`, `Project`, `NativeProjects`, `Dependencies` | Replace the fields of the descriptor |

An entry with no folder, discovered or by `Path`, declares a module that exists only as a package: it needs
`Version`, and is always linked as a package.

The commands edit the entries for you, and leave the manifest as it was when the result would be inconsistent:

```bash
dotnet stellar modules add F:/project/CS/Stellar.Logging     # Path, and a NuGet.config in the folder if it has none
dotnet stellar modules disable Stellar.Physics               # Enabled = false
dotnet stellar modules enable Stellar.Physics
dotnet stellar modules reset Stellar.Physics                 # removes the entry
```

## What is generated

`stellar modules sync` (and every command above) resolves the modules and writes into `manifests/.generated/`:

| File | For |
|---|---|
| `stellar.modules.props` | The catalog MSBuild imports, see [Modules](../Architecture/Modules.md) |
| `stellar.modules.json` | The same modules in load order, for tools that read JSON |

## Validation

The modules are rejected, and nothing is written, for:

- a descriptor that is malformed, has no `Id`, or is not named `<Id>.stellar.module`;
- a module found twice, or two entries for one module;
- `Stellar.Kernel` or `Stellar.Core` declared as a module: they are the core;
- a `Path` without the descriptor, a `Project` that does not exist, a package-only module without `Version`;
- a dependency on an unknown or a disabled module;
- a dependency on the same or a higher level: a module may only depend on strictly lower levels.
  `Runtime` is the exception, since by its nature it drives the modules above it;
- dependency cycles.

Warnings: an implicit dependency listed anyway (`Stellar.Core`), a module folder with no project at all.

The load order is deterministic: dependencies first, then by level, then by name.
