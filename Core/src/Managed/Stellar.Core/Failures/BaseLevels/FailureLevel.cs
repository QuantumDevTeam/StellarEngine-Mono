using Stellar.Core.Data.Collections;
using Stellar.Kernel;
using Stellar.Kernel.Failures;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Failures.BaseLevels;

/// <inheritdoc cref="IFailureLevel"/>
public abstract class FailureLevel
    : IFailureLevel
{
    private readonly IFailureDispatcherMeta _dispatcherMeta;

    /// <summary>
    /// Creates a level and adds it to the levels of a dispatcher.
    /// </summary>
    /// <param name="name">The name of the level.</param>
    /// <param name="dispatcherMeta">The meta of the dispatcher the level belongs to.</param>
    protected FailureLevel(string name, IFailureDispatcherMeta dispatcherMeta)
    {
        ArgumentNullException.ThrowIfNull(dispatcherMeta);

        // Levels belong to their dispatcher and two dispatchers may both have an "Error": the label is not global.
        Label = Core.Label.Create(name, UID, false)
                ?? throw new ArgumentException("A failure level needs a name", nameof(name));

        _dispatcherMeta = dispatcherMeta;
        Register(_dispatcherMeta.FailureLevels);
    }

    /// <inheritdoc/>
    public IIdentifier UID { get; } = Identifier.CreateAndRegister();

    /// <inheritdoc/>
    public ILabel Label { get; }

    /// <inheritdoc/>
    public abstract bool IsEnabled { get; set; }

    /// <inheritdoc/>
    public abstract bool IsLoggable { get; }

    /// <inheritdoc/>
    public abstract bool IsStopExecute { get; }

    /// <inheritdoc/>
    public abstract bool IsCritical { get; }

    /// <inheritdoc/>
    public abstract bool ShouldTerminate { get; }

    /// <inheritdoc/>
    public void Register(IQuantumObject registry)
    {
        if (registry is not DataContainer<IFailureLevel> container)
            throw new ArgumentException("Failure levels are stored in a container of failure levels", nameof(registry));

        container.Set(this);
    }

    /// <inheritdoc/>
    public void Unregister(IQuantumObject registry)
    {
        if (registry is not DataContainer<IFailureLevel> container)
            throw new ArgumentException("Failure levels are stored in a container of failure levels", nameof(registry));

        container.Pop(UID);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Unregister(_dispatcherMeta.FailureLevels);
    }
}
