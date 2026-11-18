# Core Architecture

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

### Level 0 - Kernel

The core of the contracts between design time, build time and runtime

- Contracts
- Decoupling dependencies
- The contracts of quants

---

### Level 1 - Stellar.Core Module

The core of the engine, the implementation of the basic concepts

- Registries
- Basic classes
- ID system

---

### Level 2 — Core Modules

They communicate only through the Kernel and Stellar.Core, completely independent of each other in Build-time,
but rely on collaboration at runtime

- Logging
- EventSystem
- JobSystem
- FileSystem
- AssetPipeline

Time and the loops are not a module: they belong to Stellar.Runtime.

---

### Level 3 — Stellar.Runtime

Creating the foundation for the engine core to work

- Loading modules
- Initialization and de-initialization
- Start and stop control
- The main loop

It is part of this repository (`Runtime/`) and the exception to the order of the levels: it depends on the Kernel
only and starts first, then loads Stellar.Core and every other module. See [Runtime](Runtime.md).

---

### Level 4 — High-Level Modules

High-level modules that provide ready-made tools for working with the engine

- Graphic
- Graphic.UI
- Stage
- Physics
- VR
- Network

### Level 5 - EndPoint

- The end user of the engine
- custom modules using the Stellar.SDK
- Game / program code
