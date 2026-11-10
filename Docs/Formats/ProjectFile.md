# `.stellar.project`

- [README.md](./../../README.md)

The configuration of a project, named `{Project}.stellar.project`. The JSON schema is
`Shared/Schemas/stellar.project.schema.json`.

```json
{
  "Project": {
    "StellarEntryPoint": "default",
    "Assets": {
      "Include": ["Assets/**"],
      "Exclude": ["**/*.tmp"],
      "Embedded": ["Assets/Icons/**"]
    },
    "Resources": {
      "Include": ["Resources/**"],
      "Exclude": [],
      "Compilers": ["Tools/MyCompiler.dll"]
    },
    "Localizations": {
      "DefaultCulture": "en",
      "Cultures": ["en", "ru"],
      "IndexFiles": ["Localization/*.stellar.loc.index"]
    }
  },
  "Runtime": {
    "Title": "My Game"
  }
}
```

All sections and lists are optional. Patterns are globs relative to the project directory. `bin`, `obj`, `.git`, `.vs`
and `.idea` are never searched.

| Field | Meaning |
|---|---|
| `StellarEntryPoint` | A full type name, or `default` for `{AssemblyName}.EntryPoint` |
| `Assets.Include` | Files copied to the output as they are |
| `Assets.Embedded` | Files embedded into the assembly. They win over `Include` |
| `Assets.Exclude` | Files removed from both lists |
| `Resources.Include` | Files compiled into `.sres` containers, in `resources/` of the output |
| `Resources.Compilers` | Assemblies with additional resource compilers |
| `Localizations.IndexFiles` | Index files. The culture is the file name before `.stellar.loc.index` |
| `Runtime` | The settings of the runtime ([Runtime](../Architecture/Runtime.md#settings)), and free-form values for modules. They reach `RuntimeConfiguration.Settings`: strings as they are, other values as JSON text |

## The runtime configuration

The build turns this file into `{Project}.stellar.runtime.config.json`, read at runtime by
`Stellar.Runtime.Configuration.RuntimeConfigurationReader`. It has a `SchemaVersion`, and every entry of `Components`
has a `Kind` that selects its type: `Assets`, `Localization`, and `Modules`, the modules the application loads, in
load order. Modules register their own kinds with `RuntimeConfigurationReader.RegisterComponent`, and unknown kinds
are skipped.

`StellarRuntimeConfigType` decides where it goes: `External`, a file next to the assembly, or `Embedded`, a resource
of the assembly. Either way the assembly carries `[assembly: StellarRuntimeConfiguration(name, source)]`, and the
runtime finds the configuration from the assembly alone.
