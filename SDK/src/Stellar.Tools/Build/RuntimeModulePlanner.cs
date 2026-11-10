using System;
using System.Collections.Generic;
using System.Linq;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Modules;

namespace Stellar.Tools.Build;

/// <summary>
/// A module of the catalog, as the build sees it.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Level">The level.</param>
/// <param name="Version">The version.</param>
/// <param name="Dependencies">The modules it depends on directly.</param>
public sealed record CatalogModule(string Id, ModuleLevel Level, string? Version, IReadOnlyList<string> Dependencies);

/// <summary>
/// Decides which modules an application loads, and in which order: everything it depends on, directly or not, every
/// module after the ones it depends on, lower levels first.
/// </summary>
public static class RuntimeModulePlanner
{
    private static IReadOnlyList<RuntimeModuleData> Order(Dictionary<string, CatalogModule> modules)
    {
        var remaining = modules.Values.ToDictionary(
            module => module.Id,
            module => module.Dependencies.Count(modules.ContainsKey),
            StringComparer.Ordinal);

        var ordered = new List<RuntimeModuleData>();

        while (remaining.Count > 0)
        {
            var next = remaining
                           .Where(pair => pair.Value == 0)
                           .Select(pair => modules[pair.Key])
                           .OrderBy(module => module.Level)
                           .ThenBy(module => module.Id, StringComparer.Ordinal)
                           .FirstOrDefault()
                       ?? throw new InvalidOperationException(
                           $"The modules depend on each other in a cycle: {string.Join(", ", remaining.Keys.OrderBy(id => id, StringComparer.Ordinal))}");

            remaining.Remove(next.Id);

            foreach (var other in remaining.Keys.ToList())
                if (modules[other].Dependencies.Contains(next.Id))
                    remaining[other]--;

            ordered.Add(new RuntimeModuleData
            {
                Id = next.Id,
                Version = next.Version,
                Level = next.Level,
                Dependencies = next.Dependencies.Where(modules.ContainsKey).ToArray()
            });
        }

        return ordered;
    }

    /// <summary>
    /// Plans the modules of an application.
    /// </summary>
    /// <param name="catalog">The modules of the installation.</param>
    /// <param name="roots">The modules the application depends on directly.</param>
    /// <param name="unknown">The modules named somewhere that the catalog does not have.</param>
    /// <returns>The modules in load order.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the modules depend on each other in a cycle.</exception>
    public static IReadOnlyList<RuntimeModuleData> Plan(IEnumerable<CatalogModule> catalog, IEnumerable<string> roots,
        out IReadOnlyList<string> unknown)
    {
        var known = new Dictionary<string, CatalogModule>(StringComparer.Ordinal);
        foreach (var module in catalog) known[module.Id] = module;

        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var needed = new Dictionary<string, CatalogModule>(StringComparer.Ordinal);
        var pending = new Stack<string>(roots.Distinct(StringComparer.Ordinal));

        while (pending.Count > 0)
        {
            var id = pending.Pop();
            if (needed.ContainsKey(id)) continue;

            if (!known.TryGetValue(id, out var module))
            {
                missing.Add(id);
                continue;
            }

            needed[id] = module;
            foreach (var dependency in module.Dependencies) pending.Push(dependency);
        }

        unknown = missing.ToList();
        return Order(needed);
    }
}
