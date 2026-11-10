namespace Stellar.Runtime.Failures;

/// <summary>
/// The levels the runtime catches at, from the outermost.
/// </summary>
public enum CatchLevel
{
    /// <summary>
    /// The runtime itself: configuration, loading of assemblies. Plain <c>try</c>/<c>catch</c>, Stellar.Core may not
    /// be loaded yet. Always fatal.
    /// </summary>
    Runtime,

    /// <summary>
    /// The initialization of the modules and of the application. Fatal: what is already up is taken down.
    /// </summary>
    Initialization,

    /// <summary>
    /// The run of the engine around the main loop, and its stop. Fatal.
    /// </summary>
    Engine,

    /// <summary>
    /// One participant of one phase of one frame. Reported; the failure system decides whether the engine stops.
    /// </summary>
    MainLoop
}
