using Stellar.Core.Data.Registry;
using Stellar.Core.Native;
using Stellar.Kernel;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core;

/// <summary>
/// A human-readable name bound to an identifier, a managed face of <c>Stellar::Native::Core::Label</c>.
/// </summary>
/// <remarks>
/// A label is a value: two labels with the same identifier and name are equal. The registry of labels exists only
/// on the native side, which owns the names.
/// </remarks>
public sealed class Label
    : ILabel, IEquatable<Label>
{
    private Label(Identifier identifier, string name)
    {
        UID = identifier;
        Name = name;
    }

    /// <inheritdoc/>
    public IIdentifier UID { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <summary>
    /// Registers the label in the native registry.
    /// </summary>
    public void Register()
    {
        Register(null);
    }

    /// <inheritdoc/>
    public void Register(IQuantumObject? registry)
    {
        RegistryOf(registry).Register(this);
    }

    /// <summary>
    /// Removes the label from the native registry.
    /// </summary>
    public void Unregister()
    {
        Unregister(null);
    }

    /// <inheritdoc/>
    public void Unregister(IQuantumObject? registry)
    {
        RegistryOf(registry).Pop(UID);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Unregister();
    }

    /// <inheritdoc/>
    public bool Equals(Label? other)
    {
        return other is not null && UID.UID == other.UID.UID && Name == other.Name;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Label);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(UID.UID, Name);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Label#{Name}";
    }

    #region Static methods

    // Overloads, not optional parameters: the C++/CLI wrapper cannot declare default values, and both wrappers
    // declare exactly these four.

    /// <summary>
    /// Creates a label bound to a new identifier and registers it.
    /// </summary>
    /// <returns>The label, or <c>null</c> if the name is blank or taken.</returns>
    public static Label? Create(string name)
    {
        return Create(name, null, true);
    }

    /// <summary>
    /// Creates a label bound to an identifier and registers it.
    /// </summary>
    /// <returns>The label, or <c>null</c> if the name is blank or taken.</returns>
    public static Label? Create(string name, IIdentifier? identifier)
    {
        return Create(name, identifier, true);
    }

    /// <summary>
    /// Creates a label bound to a new identifier.
    /// </summary>
    /// <returns>The label, or <c>null</c> if the name is blank or registration found the name already taken.</returns>
    public static Label? Create(string name, bool shouldRegister)
    {
        return Create(name, null, shouldRegister);
    }

    /// <summary>
    /// Creates a label.
    /// </summary>
    /// <param name="name">The human-readable name.</param>
    /// <param name="identifier">The identifier to bind the label to. A new one is created when null.</param>
    /// <param name="shouldRegister">Whether to put the label to the registry.</param>
    /// <returns>The label, or <c>null</c> if the name is blank or registration found the name already taken.</returns>
    public static Label? Create(string name, IIdentifier? identifier, bool shouldRegister)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var label = new Label(identifier is null ? Identifier.Create() : new Identifier(identifier.UID), name);

        if (shouldRegister && !CoreContext.Current.Labels.Register(label))
            return null;

        return label;
    }

    /// <summary>
    /// Finds a registered label by the identifier it is bound to.
    /// </summary>
    public static Label? Get(IIdentifier identifier)
    {
        return CoreContext.Current.Labels.Get(identifier);
    }

    /// <summary>
    /// Finds a registered label by its name.
    /// </summary>
    public static Label? Get(string name)
    {
        return CoreContext.Current.Labels.GetByName(name);
    }

    #endregion

    /// <summary>
    /// Finds the label with this name, creating it on first use.
    /// </summary>
    public static implicit operator Label?(string name)
    {
        return Get(name) ?? Create(name);
    }

    internal static Label FromNative(Guid identifier, string name)
    {
        return new Label(new Identifier(identifier), name);
    }

    private static IRegistry<Label> RegistryOf(IQuantumObject? registry)
    {
        return (registry ?? CoreContext.Current.Labels) as IRegistry<Label>
               ?? throw new ArgumentException($"{registry!.GetType().Name} cannot store labels", nameof(registry));
    }
}
