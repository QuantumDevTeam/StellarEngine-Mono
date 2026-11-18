<p align="center"><img src="../Shared/Branding/stellar_1x1.svg" alt="StellarEngine" width="160"/></p>

# StellarEngine-Core

The core of [StellarEngine](https://quantumdevteam.github.io/StellarEngine-Mono/): the one component installed into the
root of every installation. This site is about the core alone: how it is built, the formats it reads, the SDK it gives
modules and games, and its API. Everything about the engine as a whole (installing it, getting started, the
architecture, the modules, the orchestra) is on the [site of the engine](https://quantumdevteam.github.io/StellarEngine-Mono/).

## What the core is

| Part | What |
|---|---|
| `Stellar.Kernel` | the contracts every layer builds on, managed and native (the C ABI) |
| `Stellar.Core` | the managed core over the native one, with two interchangeable wrappers |
| `Stellar.Runtime` | loads the modules and the application, runs the stages and the main loop |
| `Stellar.Sdk` | the MSBuild SDK of modules and games, with its tasks and analyzers |
| `Stellar.Tools`, `stellar` | the tooling library and the command line over it |

## Architecture of the core

- [Overview](Architecture/Overview.md)
- [Core](Architecture/Core.md)
- [Pipeline](Architecture/Pipeline.md)
- [SDK](Architecture/Sdk.md)
- [Runtime](Architecture/Runtime.md)
- [Tools](Architecture/Tools.md)
- [Native ABI](Architecture/NativeAbi.md)

## Formats

- [Project file](Formats/ProjectFile.md)
- [Module descriptor](Formats/ModuleDescriptor.md)
- [Engine manifest](Formats/EngineManifest.md)
- [Resource container](Formats/ResourceContainer.md)

The JSON schemas of these files are published here, under [`/schemas/`](https://quantumdevteam.github.io/StellarEngine-Core/schemas/stellar.module.schema.json).

## Reference

- [API reference](xref:Stellar.Kernel), generated from the code of `Stellar.Kernel`, `Stellar.Core`, `Stellar.Runtime` and `Stellar.Tools`
- [SDK properties and items](Reference/Sdk.Properties.md)
- [Diagnostics](Reference/Diagnostics.md)
- [Migrating to 0.9](Migration-0.9.md)
- [Migrating to 0.8](Migration-0.8.md)
