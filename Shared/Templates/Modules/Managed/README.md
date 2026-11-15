# {{ModuleId}}

{{ModuleDescription}}

| | |
|---|---|
| Level | `{{ModuleLevel}}` |
| Descriptor | `{{ModuleId}}.stellar.module` |

## Layout

```
{{ModuleId}}/
├── {{ModuleId}}.stellar.module   what the engine knows about the module
├── src/Managed/{{ModuleId}}/     the C# project
├── src/Native/                   native projects, if any: built first, their DLLs travel with the assembly
├── tests/                        test projects
├── Branding/Logo.svg             the icon of the module: a placeholder, replace it with your own
└── Docs/                         the documentation site of the module (docfx.json, published by .github/workflows/docs.yml)
```

## Working on it

```bash
stellar modules build {{ModuleId}} --tests
```

A dependency on another module goes into `Dependencies` of the descriptor, then `stellar modules sync`: the
project gets the reference by itself.
