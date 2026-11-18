# Engine pipeline

- [README.md](https://github.com/QuantumDevTeam/StellarEngine-Core#readme)

## Installing & Building
```mermaid
flowchart LR
    Tools --> Kernel --> Start
    SDK --> Tools
    CLI --> Tools
    Modules --> Kernel
    Modules --> Natives --> Native.Kernel
    Modules --> SDK
    
    ExecutableModule --> GeneratedLauncher --> Stellar.Runtime --> StellarModules
```

## Runtime lifecycle

The launcher calls `StellarRuntime.Run`, which runs five stages and stops them in reverse order:

```mermaid
flowchart LR
    Configuration --> ModuleLoad --> EngineStart --> ApplicationStart --> MainLoop
```

See [Runtime](Runtime.md) for what each stage does, the loops and the exit codes.