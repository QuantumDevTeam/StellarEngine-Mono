using Stellar.Core.Quantization;
using Stellar.Kernel;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// A table with branches: rosters inside a roster, such as the teams of the players, the squads of a team.
/// </summary>
/// <remarks>
/// <para>A branch is a roster of its own: registered, found by its identifier, with its own objects. A roster never
/// holds itself, at any depth. <see cref="Find"/> looks through the roster and every branch below it.</para>
/// </remarks>
/// <typeparam name="T">The type of the objects: a base class or a contract.</typeparam>
public class Roster<T>
    : Table<T>
    where T : class, IIdentifiableQuantumObject
{
    private readonly ConcurrentMap<Identifier, Roster<T>> _branches = new();

    /// <summary>
    /// Makes a roster.
    /// </summary>
    /// <param name="items">The first objects; none if <c>null</c>.</param>
    /// <param name="meta">Its primary meta; a new one if <c>null</c>.</param>
    public Roster(IEnumerable<T>? items = null, MetaQuant? meta = null)
        : base(items, meta)
    {
    }

    /// <summary>
    /// Gets a snapshot of the branches right below.
    /// </summary>
    public IReadOnlyCollection<Roster<T>> Branches => _branches.Values;

    /// <summary>
    /// Adds a branch.
    /// </summary>
    /// <param name="branch">The roster to add below this one.</param>
    /// <returns><c>true</c> if it was added; <c>false</c> if it was there.</returns>
    /// <exception cref="ArgumentException">Thrown if the branch is this roster or holds it.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the roster is sealed.</exception>
    public bool AddBranch(Roster<T> branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        ThrowIfSealed();
        if (ReferenceEquals(branch, this) || branch.Holds(this))
            throw new ArgumentException($"{branch} holds {this}: a roster never holds itself", nameof(branch));

        return _branches.TryAdd(branch.UID, branch);
    }

    /// <summary>
    /// Makes a new branch below this roster.
    /// </summary>
    /// <param name="items">Its first objects; none if <c>null</c>.</param>
    /// <returns>The branch.</returns>
    public Roster<T> NewBranch(IEnumerable<T>? items = null)
    {
        var branch = new Roster<T>(items);
        AddBranch(branch);
        return branch;
    }

    /// <summary>
    /// Removes a branch right below.
    /// </summary>
    /// <param name="id">The identifier of the branch.</param>
    /// <returns>The removed branch, or <c>null</c> if there was none.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the roster is sealed.</exception>
    public Roster<T>? RemoveBranch(Identifier id)
    {
        ThrowIfSealed();
        return _branches.TryRemove(id, out var branch) ? branch : null;
    }

    /// <summary>
    /// Finds a branch right below.
    /// </summary>
    /// <param name="id">The identifier of the branch.</param>
    /// <param name="branch">The branch, or <c>null</c>.</param>
    /// <returns><c>true</c> if there is one.</returns>
    public bool TryGetBranch(Identifier id, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Roster<T>? branch)
    {
        return _branches.TryGet(id, out branch);
    }

    /// <summary>
    /// Gets a branch right below.
    /// </summary>
    /// <param name="id">The identifier of the branch.</param>
    /// <returns>The branch.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if there is none.</exception>
    public Roster<T> GetBranch(Identifier id)
    {
        return TryGetBranch(id, out var branch) ? branch : throw new KeyNotFoundException($"{id} is not a branch of {this}");
    }

    /// <summary>
    /// Finds an object in this roster or in any branch below it.
    /// </summary>
    /// <param name="id">The identifier of the object.</param>
    /// <returns>The object, or <c>null</c>.</returns>
    public T? Find(Identifier id)
    {
        if (TryGet(id, out var item)) return item;

        foreach (var (_, branch) in _branches.Snapshot)
            if (branch.Find(id) is { } found)
                return found;

        return null;
    }

    private bool Holds(Roster<T> roster)
    {
        foreach (var (_, branch) in _branches.Snapshot)
            if (ReferenceEquals(branch, roster) || branch.Holds(roster))
                return true;

        return false;
    }
}
