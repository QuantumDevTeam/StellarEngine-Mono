using System.Reflection;
using Stellar.Kernel.EntryPoint;

namespace Stellar.Runtime.Modules;

/// <summary>
/// Finds the entry point of a module: the class marked with <see cref="StellarEntryPointAttribute"/>, or the one the
/// runtime configuration names.
/// </summary>
public static class EntryPointLocator
{
    /// <summary>
    /// Finds the entry point of a module.
    /// </summary>
    /// <param name="assembly">The assembly of the module.</param>
    /// <param name="typeName">The full name of the class, if known.</param>
    /// <returns>The class, or <c>null</c> if the module has none.</returns>
    /// <exception cref="ModuleLoadException">
    /// Thrown if the named class is missing or is not an entry point, or if several classes are marked.
    /// </exception>
    public static Type? Find(Assembly assembly, string? typeName = null)
    {
        var moduleName = assembly.GetName().Name;

        if (!string.IsNullOrEmpty(typeName))
        {
            var type = assembly.GetType(typeName, false)
                       ?? throw new ModuleLoadException($"{moduleName} has no class {typeName}");

            return IsEntryPoint(type)
                ? type
                : throw new ModuleLoadException(
                    $"{typeName} is not an entry point: it must derive from {nameof(StellarEntryPoint)}");
        }

        var marked = LoadableTypes(assembly)
            .Where(type => type.IsDefined(typeof(StellarEntryPointAttribute), false) && IsEntryPoint(type))
            .ToList();

        return marked.Count switch
        {
            0 => null,
            1 => marked[0],
            _ => throw new ModuleLoadException(
                $"{moduleName} has several entry points: {string.Join(", ", marked.Select(type => type.FullName))}")
        };
    }

    private static bool IsEntryPoint(Type type)
    {
        return typeof(StellarEntryPoint).IsAssignableFrom(type) && !type.IsAbstract;
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
