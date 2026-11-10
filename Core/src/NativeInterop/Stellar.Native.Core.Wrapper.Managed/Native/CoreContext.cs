using System.Runtime.CompilerServices;
using Stellar.Core.Data.Registry;
using Stellar.Core.Modules;
using Stellar.Kernel.Lifecycle;

namespace Stellar.Core.Native;

/// <summary>
/// The registries of the native core for one engine: identifiers, labels, containers, native modules.
/// </summary>
/// <remarks>
/// <para>Every engine instance has its own context (<see cref="For"/>), so two engines in one process never see
/// each other's identifiers or labels. <see cref="Current"/> is the context of the engine of the running code
/// (<see cref="EngineScope"/>), or <see cref="Default"/> outside any engine.</para>
/// <para>A context made with the constructor belongs to its maker, who disposes it.</para>
/// </remarks>
public sealed unsafe class CoreContext
    : IDisposable
{
    private static readonly ConditionalWeakTable<IEngineInstance, CoreContext> Engines = new();

    private nint _handle;
    private readonly bool _owned;

    private CoreContext(nint handle, bool owned)
    {
        _handle = handle;
        _owned = owned;
        Identifiers = new IdentifierRegistry(this);
        Labels = new LabelRegistry(this);
        Containers = new ContainerRegistry(this);
        Modules = new ModuleRegistry(this);
    }

    /// <summary>Creates a context with empty registries.</summary>
    public CoreContext()
        : this(Create(), true)
    {
    }

    /// <summary>Finalizes the context.</summary>
    ~CoreContext()
    {
        Destroy();
    }

    /// <summary>Gets the default context of the process: for code outside any engine.</summary>
    public static CoreContext Default { get; } = new(0, false);

    /// <summary>Gets the context of the engine of the running code, or the default one.</summary>
    public static CoreContext Current => EngineScope.Current is { } engine ? For(engine) : Default;

    /// <summary>Gets whether this is the default context.</summary>
    public bool IsDefault => !_owned;

    /// <summary>Gets whether the context was disposed.</summary>
    public bool IsDisposed => _owned && _handle == 0;

    /// <summary>Gets the registry of identifiers.</summary>
    public IdentifierRegistry Identifiers { get; }

    /// <summary>Gets the registry of labels.</summary>
    public LabelRegistry Labels { get; }

    /// <summary>Gets the registry of the membership of containers.</summary>
    public ContainerRegistry Containers { get; }

    /// <summary>Gets the registry of native modules.</summary>
    public ModuleRegistry Modules { get; }

    /// <summary>The handle the exports take; 0 for the default context.</summary>
    internal nint Handle => IsDisposed ? throw new ObjectDisposedException(nameof(CoreContext)) : _handle;

    /// <summary>Gets the context of an engine, created the first time.</summary>
    /// <param name="engine">The engine.</param>
    /// <returns>Its context.</returns>
    public static CoreContext For(IEngineInstance engine)
    {
        ArgumentNullException.ThrowIfNull(engine);
        return Engines.GetValue(engine, static _ => new CoreContext());
    }

    /// <summary>Disposes the context of an engine, if it has one.</summary>
    /// <param name="engine">The engine.</param>
    /// <returns>Whether it had one.</returns>
    public static bool Release(IEngineInstance engine)
    {
        if (engine is null || !Engines.TryGetValue(engine, out var context)) return false;

        Engines.Remove(engine);
        context.Dispose();
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Destroy();
        GC.SuppressFinalize(this);
    }

    private static nint Create()
    {
        nint handle;
        Exports.Check(Exports.stellar_core_context_create(&handle));
        return handle;
    }

    private void Destroy()
    {
        if (!_owned) return;

        var handle = Interlocked.Exchange(ref _handle, 0);
        if (handle != 0) Exports.stellar_core_context_destroy(handle);
    }
}
