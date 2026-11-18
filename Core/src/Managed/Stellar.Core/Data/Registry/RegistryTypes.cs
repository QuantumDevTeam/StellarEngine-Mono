using System.Collections.Concurrent;
using System.Reflection;
using Stellar.Kernel.Data.Registry;
using Stellar.Kernel.Quantization;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// The types whose registries an object goes into implicitly: the types of its hierarchy marked with
/// <see cref="StellarRegistryAttribute"/>.
/// </summary>
/// <remarks>
/// Read once per type and kept: this is metadata of types, the same for every engine.
/// </remarks>
public static class RegistryTypes
{
    private static readonly ConcurrentDictionary<Type, Type[]> Marked = new();

    /// <summary>
    /// Gets the marked types of a hierarchy: its classes from the type up, then its contracts.
    /// </summary>
    /// <param name="type">The type of an object.</param>
    /// <returns>The marked types, each once.</returns>
    public static IReadOnlyList<Type> MarkedIn(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Marked.GetOrAdd(type, static type =>
        {
            var classes = new List<Type>();
            for (var current = type; current is not null; current = current.BaseType)
                if (IsMarked(current)) classes.Add(current);

            var contracts = type.GetInterfaces().Where(IsMarked).OrderBy(contract => contract.FullName, StringComparer.Ordinal);
            return [.. classes, .. contracts];
        });
    }

    /// <summary>
    /// Checks that a type can have a registry: a class or a contract of registrable objects.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <exception cref="ArgumentException">Thrown if it cannot.</exception>
    public static void Validate(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!typeof(IRegistrableQuantumObject).IsAssignableFrom(type) || type.IsValueType || type.ContainsGenericParameters)
            throw new ArgumentException($"{type.Name} cannot have a registry: it must be a class or a contract of IRegistrableQuantumObject",
                nameof(type));
    }

    private static bool IsMarked(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return definition.GetCustomAttribute<StellarRegistryAttribute>(inherit: false) is not null;
    }
}
