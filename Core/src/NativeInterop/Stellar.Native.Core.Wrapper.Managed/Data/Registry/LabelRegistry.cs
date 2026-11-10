using System.Text;
using Stellar.Core.Native;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Interop;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// The registry of labels of a <see cref="CoreContext"/>: names are unique within it. It holds no state: it is a
/// view of the native registry.
/// </summary>
public sealed unsafe class LabelRegistry
    : IRegistry<Label>
{
    private readonly CoreContext _context;

    internal LabelRegistry(CoreContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public bool Exists(IIdentifier id)
    {
        if (id is null) return false;

        var uid = id.UID;
        int contained;
        return Exports.Ask(Exports.stellar_label_contains(_context.Handle, &uid, &contained), contained);
    }

    /// <inheritdoc/>
    public bool Register(Label obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var bytes = Encoding.UTF8.GetBytes(obj.Name);
        var uid = obj.UID.UID;

        fixed (byte* data = bytes)
        {
            int added;
            return Exports.Ask(
                Exports.stellar_label_register(_context.Handle, new Exports.StringView(data, bytes.Length), &uid,
                    &added),
                added);
        }
    }

    /// <inheritdoc/>
    public Label? Get(IIdentifier id)
    {
        if (id is null) return null;

        var name = NameOf(id.UID);
        return name is null ? null : Label.FromNative(id.UID, name);
    }

    /// <summary>
    /// Gets a label by name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The label, or <c>null</c> if no label has this name.</returns>
    public Label? GetByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var bytes = Encoding.UTF8.GetBytes(name);

        fixed (byte* data = bytes)
        {
            Guid uid;
            int found;
            Exports.Check(Exports.stellar_label_find(_context.Handle, new Exports.StringView(data, bytes.Length), &uid,
                &found));

            return found != 0 ? Label.FromNative(uid, name) : null;
        }
    }

    /// <inheritdoc/>
    public Label? Pop(IIdentifier id)
    {
        var label = Get(id);
        if (label is null) return null;

        var uid = id.UID;
        int removed;
        return Exports.Ask(Exports.stellar_label_unregister(_context.Handle, &uid, &removed), removed) ? label : null;
    }

    /// <inheritdoc/>
    public int Size
    {
        get
        {
            nuint count;
            Exports.Check(Exports.stellar_label_count(_context.Handle, &count));
            return (int)count;
        }
    }

    /// <inheritdoc/>
    public ICollection<IIdentifier> Keys => throw new NotSupportedException("Labels are looked up, not enumerated");

    /// <inheritdoc/>
    public ICollection<Label> Values => throw new NotSupportedException("Labels are looked up, not enumerated");

    private string? NameOf(Guid uid)
    {
        var context = _context.Handle;

        try
        {
            return Exports.ReadText((buffer, capacity, required) =>
            {
                var id = uid;
                return Exports.stellar_label_get_name(context, &id, buffer, capacity, required);
            });
        }
        catch (NativeCoreException exception) when (exception.Result == NativeResult.NotFound)
        {
            return null;
        }
    }
}
