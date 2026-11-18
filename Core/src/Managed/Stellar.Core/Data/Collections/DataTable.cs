using Stellar.Core.Quantization;

namespace Stellar.Core.Data.Collections;

/// <summary>
/// Rows of one type by name: tuning, loot, dialogue lines, the stats of the units.
/// </summary>
/// <remarks>
/// <para>A container like the others: registered, found by its identifier, sealable. The rows are values of any
/// type, a record or a struct, with no identity of their own: a row is its name. Names compare by ordinal and are
/// not blank.</para>
/// <para>Filled in code for now; loading tables from files comes with the assets.</para>
/// </remarks>
/// <typeparam name="TRow">The type of a row.</typeparam>
public class DataTable<TRow>
    : DataContainer<string, TRow>
{
    /// <summary>
    /// Makes a data table.
    /// </summary>
    /// <param name="rows">The first rows, by name; none if <c>null</c>.</param>
    /// <param name="meta">Its primary meta; a new one if <c>null</c>.</param>
    public DataTable(IEnumerable<KeyValuePair<string, TRow>>? rows = null, MetaQuant? meta = null)
        : base(meta, StringComparer.Ordinal)
    {
        if (rows is null) return;
        foreach (var (name, row) in rows) Add(name, row);
    }

    /// <summary>
    /// Gets the type of a row.
    /// </summary>
    public Type RowType => typeof(TRow);

    /// <summary>
    /// Adds a row.
    /// </summary>
    /// <param name="name">Its name, not blank.</param>
    /// <param name="row">The row.</param>
    /// <returns><c>true</c> if it was added; <c>false</c> if a row has the name.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the table is sealed.</exception>
    public bool Add(string name, TRow row)
    {
        ThrowIfSealed();
        return Items.TryAdd(NameOf(name), row);
    }

    /// <summary>
    /// Puts a row in, replacing the one with the name.
    /// </summary>
    /// <param name="name">Its name, not blank.</param>
    /// <param name="row">The row.</param>
    /// <exception cref="InvalidOperationException">Thrown if the table is sealed.</exception>
    public void Set(string name, TRow row)
    {
        ThrowIfSealed();
        Items.Set(NameOf(name), row);
    }

    /// <summary>
    /// Removes a row.
    /// </summary>
    /// <param name="name">Its name.</param>
    /// <returns><c>true</c> if there was one.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the table is sealed.</exception>
    public bool Remove(string name)
    {
        ThrowIfSealed();
        return Items.Remove(name);
    }

    private static string NameOf(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A row has a name", nameof(name));
        return name;
    }
}
