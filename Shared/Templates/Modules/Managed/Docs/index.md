# {{ModuleId}}

{{ModuleDescription}}

A `{{ModuleLevel}}` module of [StellarEngine]({{EngineSiteUri}}). Its site is {{ModuleSiteUri}}, built from the Markdown
files of `Docs/` with DocFX; the whole engine, with every module, is on the [site of the engine]({{EngineSiteUri}}).

## Using it

Add `{{ModuleId}}` to `Dependencies` of your module or project descriptor, then:

```powershell
stellar modules sync
```

## Working on it

```powershell
stellar modules build {{ModuleId}} --tests
```

See [the README](../README.md) for the layout of the module.
