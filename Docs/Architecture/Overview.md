# Architecture Overview

- [README.md](./../../README.md)

```mermaid
flowchart LR
    Tools --> Kernel
    CLI --> Tools
    SDK --> Tools
    Editor --> Tools
    Engine --> Modules --> Kernel
    Editor --> Engine
```

## Kernel

The basic contract layer is used to divide the engine into modules and help combine them into one structure.\
> The Kernel is not a module!\
> The Kernel is the engine layer present in all parts of the engine

- Stellar.The kernel is a basic core that has all the necessary contracts for Design-time, Build-time, and Core engine modules (including
  Stellar.Core)
- Stellar.{Module}.Kernel - The contract kernel for a specific High-level engine module
  What is it for, an example:

```mermaid
flowchart LR
    Stellar.EventSystem --> Stellar.Logging --> Stellar.FileSystem --> Stellar.EventSystem
```

Ring dependency in modules is exactly what the Kernel solves. The first module with business logic helps him.
Stellar.Core

```mermaid
flowchart LR
    Stellar.EventSystem --> Stellar.Core
    Stellar.EventSystem --- Stellar.Kernel
    Stellar.Logging --- Stellar.Kernel
    Stellar.Logging --> Stellar.Core
    Stellar.FileSystem --- Stellar.Kernel
    Stellar.FileSystem --> Stellar.Core
    Stellar.Core ==> Stellar.Kernel
```

## Modules

The engine modules are provided by 4 main levels (where 0 is the Kernel):

0) Kernel - see above
1) Core module - the first module that serves as the core of the entire system.
2) Core modules - several separate systems divided into independent modules
3) Stellar.Runtime - a system that controls the behavior of the engine during execution
4) High-Level modules - the main modules representing the main API
   Each of these levels can only communicate directly with the lower ones, i.e. Core cannot access Stellar.Graphic.
   The only exception is Stellar.Runtime due to the specifics of its basic concept.

## Engine

The Engine itself is a separate project, a collection that provides basic interfaces for connecting to the initialization
of the engine and its main cycle.

## Tools

The library with the logic of everything that runs outside of a game, see [Tools](Tools.md). It knows the contracts and
works with them:

- The layout of an installation, the engine manifest and the module graph
- Project configuration and templates
- The resource pipeline, the part of the AssetPipeline that compiles sources into the engine format
- The runtime configuration and the launcher

## CLI

A simple .NET tool, a thin wrapper on top of Tools.

## SDK

A wrapper on top of the .NET SDK, see [SDK](Sdk.md). It simplifies the project build by connecting the necessary set of
modules and libraries, against sources in Debug and against the local NuGet feed in Release.\
The SDK also includes the Roslyn analyzer, which keeps the code that works with the engine correct.
In the future a source generator can help with the integration of C# scripts.

## Editor

This is an example of an engine consumer. Hyper-modularity and re-usability will have to make it possible to write an Editor directly on
the engine itself. This action should help with the integration of Debugger and Profiler projects, but also with other non-obvious ones.
tasks. An example is the simplification of embedding a game artifact in the editor and vice versa.

## SSK - Stellar Software Kit

All this adds up to the SSK (or SSDK - Stellar Software Development Kit), a set of tools to simplify
the integration of the engine and its use in development