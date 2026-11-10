namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// The stages of the runtime lifecycle described in Docs/Architecture/Pipeline.md.
/// </summary>
public enum EngineState
{
    /// <summary>Nothing is loaded yet.</summary>
    Created,

    /// <summary>Modules are being loaded and initialized.</summary>
    Initializing,

    /// <summary>The main loop is running.</summary>
    Running,

    /// <summary>The main loop is paused.</summary>
    Paused,

    /// <summary>A stop was requested and modules are being unloaded.</summary>
    Stopping,

    /// <summary>The runtime finished its cleanup.</summary>
    Stopped
}
