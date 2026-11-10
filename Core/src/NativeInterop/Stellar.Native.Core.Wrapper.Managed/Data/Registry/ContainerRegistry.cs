using Stellar.Core.Native;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// The membership of the data containers of a <see cref="CoreContext"/>: which identifiers and which branches each
/// container holds. The values stay in C#.
/// </summary>
public sealed unsafe class ContainerRegistry
{
    private readonly CoreContext _context;

    internal ContainerRegistry(CoreContext context)
    {
        _context = context;
    }

    /// <summary>Creates a container. <returns>False if it exists already.</returns></summary>
    public bool Create(Guid container)
    {
        int created;
        return Exports.Ask(Exports.stellar_container_create(_context.Handle, &container, &created), created);
    }

    /// <summary>Destroys a container. <returns>False if it does not exist.</returns></summary>
    public bool Destroy(Guid container)
    {
        int destroyed;
        return Exports.Ask(Exports.stellar_container_destroy(_context.Handle, &container, &destroyed), destroyed);
    }

    /// <summary>Checks for a container.</summary>
    public bool Exists(Guid container)
    {
        int exists;
        return Exports.Ask(Exports.stellar_container_exists(_context.Handle, &container, &exists), exists);
    }

    /// <summary>Seals a container: it cannot change any more.</summary>
    public void Seal(Guid container)
    {
        Exports.Check(Exports.stellar_container_seal(_context.Handle, &container));
    }

    /// <summary>Checks whether a container is sealed.</summary>
    public bool IsSealed(Guid container)
    {
        int isSealed;
        return Exports.Ask(Exports.stellar_container_is_sealed(_context.Handle, &container, &isSealed), isSealed);
    }

    /// <summary>Adds an item. <returns>False if it is there already.</returns></summary>
    public bool Add(Guid container, Guid item)
    {
        int added;
        return Exports.Ask(Exports.stellar_container_add(_context.Handle, &container, &item, &added), added);
    }

    /// <summary>Removes an item. <returns>False if it is not there.</returns></summary>
    public bool Remove(Guid container, Guid item)
    {
        int removed;
        return Exports.Ask(Exports.stellar_container_remove(_context.Handle, &container, &item, &removed), removed);
    }

    /// <summary>Checks for an item.</summary>
    public bool Contains(Guid container, Guid item)
    {
        int contained;
        return Exports.Ask(Exports.stellar_container_contains(_context.Handle, &container, &item, &contained),
            contained);
    }

    /// <summary>Gets the number of items.</summary>
    public int Count(Guid container)
    {
        nuint count;
        Exports.Check(Exports.stellar_container_count(_context.Handle, &container, &count));
        return (int)count;
    }

    /// <summary>Gets the items.</summary>
    public Guid[] Items(Guid container)
    {
        var context = _context.Handle;
        return Exports.ReadIdentifiers((buffer, capacity, required) =>
        {
            var id = container;
            return Exports.stellar_container_items(context, &id, buffer, capacity, required);
        });
    }

    /// <summary>Removes every item. <returns>False if the container does not exist.</returns></summary>
    public bool Clear(Guid container)
    {
        int cleared;
        return Exports.Ask(Exports.stellar_container_clear(_context.Handle, &container, &cleared), cleared);
    }

    /// <summary>Adds a branch. <returns>False if it is there already.</returns></summary>
    public bool AddBranch(Guid container, Guid branch)
    {
        int added;
        return Exports.Ask(Exports.stellar_container_add_branch(_context.Handle, &container, &branch, &added), added);
    }

    /// <summary>Removes a branch. <returns>False if it is not there.</returns></summary>
    public bool RemoveBranch(Guid container, Guid branch)
    {
        int removed;
        return Exports.Ask(Exports.stellar_container_remove_branch(_context.Handle, &container, &branch, &removed),
            removed);
    }

    /// <summary>Checks for a branch.</summary>
    public bool ContainsBranch(Guid container, Guid branch)
    {
        int contained;
        return Exports.Ask(Exports.stellar_container_contains_branch(_context.Handle, &container, &branch, &contained),
            contained);
    }

    /// <summary>Gets the branches.</summary>
    public Guid[] Branches(Guid container)
    {
        var context = _context.Handle;
        return Exports.ReadIdentifiers((buffer, capacity, required) =>
        {
            var id = container;
            return Exports.stellar_container_branches(context, &id, buffer, capacity, required);
        });
    }
}
