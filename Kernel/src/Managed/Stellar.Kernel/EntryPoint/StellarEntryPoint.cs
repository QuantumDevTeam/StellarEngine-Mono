using System;
using System.Collections.Generic;
using Stellar.Kernel.Configuration;
using Stellar.Kernel.Quantization;

namespace Stellar.Kernel.EntryPoint;

/// <summary>
/// Abstract base class for all application entry points in the Stellar Engine.
/// </summary>
/// <remarks>
/// <para>An entry point is the starting and stopping unit of a module or application. Like everything built on the
/// engine it is a quant, and it exists before any registry does: this is why it carries its own minimal
/// identifier and label instead of the ones from Stellar.Core.</para>
/// <para>Derived classes must implement <see cref="Run{TContext}"/> and <see cref="RequestStop{TContext}"/>. The
/// engine calls these methods with contexts that contain logging and failure handling.</para>
/// <para>The entry point's <see cref="UID"/> is generated randomly and must not be used for persistent identification.
/// Use the <see cref="ILabel.Name"/> of <see cref="Label"/> for that.</para>
/// </remarks>
public abstract class StellarEntryPoint
    : IQuant, ILabeled, IDisposable
{
    private readonly EntryPointMeta _meta;
    private readonly IMetaQuant[] _metas;

    /// <summary>
    /// The runtime configuration assigned to this entry point during initialization.
    /// </summary>
    public RuntimeConfiguration RuntimeConfiguration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StellarEntryPoint"/> class.
    /// </summary>
    /// <param name="name">The human-readable name of the entry point.</param>
    /// <param name="runtimeConfiguration">The runtime configuration obtained during engine initialization.</param>
    /// <exception cref="ArgumentNullException">Thrown if an argument is <c>null</c>.</exception>
    protected StellarEntryPoint(string name, RuntimeConfiguration runtimeConfiguration)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        RuntimeConfiguration = runtimeConfiguration ?? throw new ArgumentNullException(nameof(runtimeConfiguration));
        _meta = new EntryPointMeta(name);
        _metas = [_meta];
    }

    /// <summary>
    /// Gets the randomly generated identifier of this entry point. It is not stable across runs.
    /// </summary>
    public IIdentifier UID => _meta.UID;

    /// <summary>
    /// Gets the human-readable label of this entry point.
    /// </summary>
    public ILabel Label => _meta;

    /// <inheritdoc/>
    public IMetaQuant Meta => _meta;

    /// <inheritdoc/>
    public IReadOnlyList<IMetaQuant> Metas => _metas;

    /// <inheritdoc/>
    public TMeta? FindMeta<TMeta>()
        where TMeta : class, IMetaQuant
    {
        return _meta as TMeta;
    }

    /// <summary>
    /// Executes the entry point's main logic.
    /// </summary>
    /// <param name="context">The execution context with logging and failure handling.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    /// <returns>A simple return code (0 typically indicates success).</returns>
    public abstract int Run<TContext>(ref readonly TContext context)
        where TContext : IRunContext, allows ref struct;

    /// <summary>
    /// Requests the entry point to stop execution gracefully.
    /// </summary>
    /// <param name="context">The stopping context containing the stop reason.</param>
    /// <typeparam name="TContext">Type of the context.</typeparam>
    public abstract void RequestStop<TContext>(ref readonly TContext context)
        where TContext : IStopContext, allows ref struct;

    /// <summary>
    /// Disposes the entry point, releasing all managed and unmanaged resources.
    /// </summary>
    public abstract void Dispose();

    private sealed class EntryPointIdentifier
        : IIdentifier
    {
        public Guid UID { get; } = Guid.NewGuid();

        public void Register(IQuantumObject registry)
        {
            throw new NotSupportedException("Entry point identifiers are not registrable");
        }

        public void Unregister(IQuantumObject registry)
        {
            throw new NotSupportedException("Entry point identifiers are not registrable");
        }

        public void Dispose()
        {
        }
    }

    private sealed class EntryPointMeta(string name)
        : ILabel
    {
        public IIdentifier UID { get; } = new EntryPointIdentifier();

        public string Name { get; } = name;

        public void Register(IQuantumObject registry)
        {
            throw new NotSupportedException("Entry point labels are not registrable");
        }

        public void Unregister(IQuantumObject registry)
        {
            throw new NotSupportedException("Entry point labels are not registrable");
        }

        public void Dispose()
        {
        }
    }
}
