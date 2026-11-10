using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// Represents a quantum object that can contain and manage other identifiable quantum objects.
/// Roster can store other Rosters in the Graph structure.
/// </summary>
/// <remarks>The tree of branches is native as well: native systems walk it by identifiers.</remarks>
/// <inheritdoc/>
public class Roster<T>
    : WritableTable<T>
    where T : IIdentifiableQuantumObject
{
    #region Constructors

    /// <summary>
    /// Creates a roster with typed data and branches.
    /// </summary>
    public Roster(MetaQuant meta, FastConcurrentIdentifierMap<T> data,
        IEnumerable<KeyValuePair<IIdentifier, Roster<T>>> branches)
        : base(meta, data)
    {
        Branches = new ConcurrentIdentifierMap<Roster<T>>();

        foreach (var branch in branches)
            SaveBranch(branch.Value);
    }

    /// <summary>
    /// Creates a roster with data and branches.
    /// </summary>
    public Roster(MetaQuant metaData, Dictionary<IIdentifier, T> data,
        IEnumerable<KeyValuePair<IIdentifier, Roster<T>>> branches)
        : this(metaData, new FastConcurrentIdentifierMap<T>(data), branches)
    {
    }

    /// <summary>
    /// Creates a roster with data.
    /// </summary>
    public Roster(MetaQuant metaData, Dictionary<IIdentifier, T> data)
        : this(metaData, data, new Dictionary<IIdentifier, Roster<T>>())
    {
    }

    /// <summary>
    /// Creates an empty roster.
    /// </summary>
    public Roster(MetaQuant metaData)
        : this(metaData, new Dictionary<IIdentifier, T>())
    {
    }

    /// <summary>
    /// Creates an empty roster with a new meta quant.
    /// </summary>
    public Roster()
        : this(new MetaQuant())
    {
    }

    #endregion

    /// <summary>
    /// Gets the managed objects of the nested rosters.
    /// </summary>
    public ConcurrentIdentifierMap<Roster<T>> Branches { get; }

    #region Branch operations

    /// <summary>
    /// Gets a branch.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown if there is no such branch.</exception>
    public Roster<T> GetBranch(IIdentifier identifier)
    {
        return Branches.GetValueOrDefault(identifier) ??
               throw new KeyNotFoundException($"{identifier} is not a branch");
    }

    /// <summary>
    /// Adds a branch.
    /// </summary>
    /// <returns><c>false</c> if it is already there.</returns>
    public bool SaveBranch(Roster<T> branch)
    {
        ArgumentNullException.ThrowIfNull(branch);

        if (!Native.AddBranch(UID.UID, branch.UID.UID))
            return false;

        return Branches.TryAdd(branch.UID, branch);
    }

    /// <summary>
    /// Removes a branch.
    /// </summary>
    /// <returns>The branch, or <c>null</c> if there is no such branch.</returns>
    public Roster<T>? PopBranch(IIdentifier identifier)
    {
        if (identifier is null || !Native.RemoveBranch(UID.UID, identifier.UID))
            return null;

        Branches.TryRemove(identifier, out var branch);
        return branch;
    }

    /// <summary>
    /// Creates and adds a branch with data.
    /// </summary>
    public Roster<T> NewBranch(MetaQuant meta, Dictionary<IIdentifier, T> data)
    {
        var branch = new Roster<T>(meta, data);
        SaveBranch(branch);
        return branch;
    }

    /// <summary>
    /// Creates and adds an empty branch.
    /// </summary>
    public Roster<T> NewBranch(MetaQuant metaData)
    {
        return NewBranch(metaData, new Dictionary<IIdentifier, T>());
    }

    /// <summary>
    /// Checks for a branch.
    /// </summary>
    public bool ContainsBranch(IIdentifier identifier)
    {
        return identifier is not null && Native.ContainsBranch(UID.UID, identifier.UID);
    }

    #endregion
}
