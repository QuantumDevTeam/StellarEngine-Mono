namespace Stellar.Kernel.Lifecycle;

/// <summary>
/// The phases of one frame of a main loop, in execution order.
/// </summary>
public enum FramePhase
{
    /// <summary>Preparation before the update.</summary>
    PreUpdate,

    /// <summary>
    /// The fixed steps: as many as the clock of each participant has run fixed steps since the previous frame
    /// (<see cref="IFixedUpdatable"/>).
    /// </summary>
    FixedUpdate,

    /// <summary>The main update.</summary>
    Update,

    /// <summary>Post-processing after the update.</summary>
    PostUpdate,

    /// <summary>Synchronization with the native side before rendering.</summary>
    PreRender,

    /// <summary>The main render.</summary>
    Render,

    /// <summary>Flush and cleanup after rendering.</summary>
    PostRender
}
