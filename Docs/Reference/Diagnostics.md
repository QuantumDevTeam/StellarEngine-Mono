# Diagnostics

- [README.md](./../../README.md)

## Analyzers (`STLR`)

Shipped inside Stellar.Sdk. Every rule can be tuned in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.STLR0202.severity = error
dotnet_diagnostic.STLR0104.severity = none
```

All of them are switched off with `StellarAnalyzers=false`.

| Id | Severity | Rule |
|---|---|---|
| STLR0101 | Error | The entry point of `.stellar.project` does not exist |
| STLR0102 | Error | The entry point does not inherit `StellarEntryPoint` |
| STLR0103 | Error | The entry point has no `[StellarEntryPoint]` |
| STLR0104 | Warning | A project with a `.stellar.project` has no entry point at all |
| STLR0201 | Error | A module depends on a module of the same or a higher level. Only direct dependencies are checked; `Runtime` may reach up, `EndPoint` may use `EndPoint` |
| STLR0202 | Warning | A module references a module its descriptor does not list in `Dependencies` |
| STLR0203 | Warning | The `Id` of the `.stellar.module` is not the module the project builds |
| STLR0301 | Error | A `.stellar.project` or `.stellar.module` is not well-formed JSON; the location points at the mistake |

The level of a project comes from the catalog (`StellarModuleLevel`), or from its kind for a project that is not a
module: `EngineCoreModule` is `CoreModule`, `EngineModule` is `HighLevel`, `Module` and `ExecutableModule` are
`EndPoint`. The level of a reference comes from its `[AssemblyMetadata("StellarModuleLevel")]`: assemblies without
it are not modules, and are not checked.

## Build checks (`STLRB`)

Raised by the targets of the SDK before compiling. They follow the usual MSBuild rules (`NoWarn`,
`WarningsAsErrors`), and are switched off with `StellarValidateProjectFile=false`.

| Id | Severity | Check |
|---|---|---|
| STLRB001 | Error | No `StellarSdkUsage`, and the project is not a module the catalog knows |
| STLRB002 | Error | An unknown `StellarSdkUsage` |
| STLRB003 | Error | `EngineCore` outside the core: a core system is `EngineCoreModule` |
| STLRB004 | Error | A required `.stellar.project` is missing |
| STLRB005 | Error | A game does not reference `Stellar.Runtime`, which its launcher calls (`StellarImplicitRuntimeReference=false`) |
| STLRB006 | Error | The apphost of .NET for the runtime identifier of a game is missing: its executable cannot be created |
| STLRB101 | Warning | A `.stellar.module` above the project describes it, but the catalog does not know it: run `stellar modules sync` |
| STLRB102 | Warning | The `.stellar.module` changed after the catalog was generated: run `stellar modules sync` |
| STLRB103 | Warning | The kind set in the project does not match the level of the module |
