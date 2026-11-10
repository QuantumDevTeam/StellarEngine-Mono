# {{ModuleId}}

{{ModuleDescription}}

| | |
|---|---|
| Level | `{{ModuleLevel}}` |
| Descriptor | [`{{ModuleId}}.stellar.module`]({{ModuleId}}.stellar.module) |

## Layout

```
{{ModuleId}}/
├── {{ModuleId}}.stellar.module   what the engine knows about the module
├── src/Managed/{{ModuleId}}/     the C# project
├── src/Native/                   native projects, if any: built first, their DLLs travel with the assembly
└── tests/                        test projects
```

## Working on it

```bash
dotnet stellar modules build {{ModuleId}} --tests
```

A dependency on another module goes into `Dependencies` of the descriptor, then `dotnet stellar modules sync`: the
project gets the reference by itself.
