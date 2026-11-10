namespace Stellar.Kernel.Modules;

/// <summary>
/// Architectural level of a module. A module may depend only on modules of a strictly lower level.
/// </summary>
/// <remarks>
/// See Docs/Architecture/Core.md. <see cref="Runtime"/> is the single exception to the rule: by its nature the
/// runtime system loads and drives the modules above it.
/// </remarks>
public enum ModuleLevel
{
    /// <summary>Level 0: the contract layer.</summary>
    Kernel = 0,

    /// <summary>Level 1: Stellar.Core, the implementation of the basic concepts.</summary>
    Core = 1,

    /// <summary>Level 2: independent core systems (logging, events, jobs, files, time).</summary>
    CoreModule = 2,

    /// <summary>Level 3: Stellar.RuntimeSystem, loading and lifecycle control.</summary>
    Runtime = 3,

    /// <summary>Level 4: High-Level Modules (graphics, physics, UI, networking).</summary>
    HighLevel = 4,

    /// <summary>Level 5: the end user of the engine - a game, an editor, a custom module.</summary>
    EndPoint = 5
}
