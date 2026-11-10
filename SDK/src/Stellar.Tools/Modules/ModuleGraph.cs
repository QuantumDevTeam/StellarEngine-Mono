using System;
using System.Collections.Generic;
using System.Linq;
using Stellar.Kernel.Modules;

namespace Stellar.Tools.Modules;

/// <summary>
/// The result of analyzing a set of modules.
/// </summary>
/// <param name="LoadOrder">The modules in dependency order. Empty when <paramref name="Errors"/> is not.</param>
/// <param name="Errors">What is wrong with the set.</param>
public sealed record ModuleGraphResult(IReadOnlyList<ModuleDescriptor> LoadOrder, IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Gets a value indicating whether the set of modules is consistent.
    /// </summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Checks the architecture rules of a set of modules and computes their load order.
/// </summary>
public static class ModuleGraph
{
    /// <summary>
    /// Analyzes a set of modules. Checks for duplicates, unknown dependencies, cycles and for the level rule:
    /// a module depends only on strictly lower levels, except for the runtime system.
    /// </summary>
    /// <param name="modules">The modules.</param>
    /// <returns>The load order, or the list of problems.</returns>
    public static ModuleGraphResult Analyze(IEnumerable<ModuleDescriptor> modules)
    {
        var errors = new List<string>();
        var byId = new Dictionary<string, ModuleDescriptor>(StringComparer.Ordinal);

        foreach (var module in modules)
            if (string.IsNullOrWhiteSpace(module.Id))
                errors.Add("A module has no Id");
            else if (byId.ContainsKey(module.Id))
                errors.Add($"Module {module.Id} is declared twice");
            else
                byId.Add(module.Id, module);

        foreach (var module in byId.Values)
        foreach (var dependencyId in module.Dependencies)
            if (!byId.TryGetValue(dependencyId, out var dependency))
                errors.Add($"{module.Id} depends on unknown module {dependencyId}");
            else if (module.Level != ModuleLevel.Runtime && dependency.Level >= module.Level)
                errors.Add($"{module.Id} ({module.Level}) must not depend on {dependencyId} ({dependency.Level})");

        if (errors.Count != 0)
            return new ModuleGraphResult(Array.Empty<ModuleDescriptor>(), errors);

        return Sort(byId);
    }

    private static ModuleGraphResult Sort(Dictionary<string, ModuleDescriptor> byId)
    {
        var pendingDependencies =
            byId.Values.ToDictionary(module => module.Id, module => module.Dependencies.Distinct().Count());
        var dependents = byId.Keys.ToDictionary(id => id, _ => new List<string>());

        foreach (var module in byId.Values)
        foreach (var dependencyId in module.Dependencies.Distinct())
            dependents[dependencyId].Add(module.Id);

        // Ties are broken by level and then by name, so the order never depends on the manifest layout.
        var ready = new SortedSet<ModuleDescriptor>(
            byId.Values.Where(module => pendingDependencies[module.Id] == 0),
            Comparer<ModuleDescriptor>.Create(CompareForLoading));

        var order = new List<ModuleDescriptor>(byId.Count);

        while (ready.Count != 0)
        {
            var next = ready.Min!;
            ready.Remove(next);
            order.Add(next);

            foreach (var dependentId in dependents[next.Id])
                if (--pendingDependencies[dependentId] == 0)
                    ready.Add(byId[dependentId]);
        }

        if (order.Count == byId.Count)
            return new ModuleGraphResult(order, Array.Empty<string>());

        var cycle = pendingDependencies.Where(pair => pair.Value > 0).Select(pair => pair.Key)
            .OrderBy(id => id, StringComparer.Ordinal);
        return new ModuleGraphResult(
            Array.Empty<ModuleDescriptor>(),
            new[] { $"Dependency cycle between: {string.Join(", ", cycle)}" });
    }

    private static int CompareForLoading(ModuleDescriptor left, ModuleDescriptor right)
    {
        var byLevel = left.Level.CompareTo(right.Level);
        return byLevel != 0 ? byLevel : string.CompareOrdinal(left.Id, right.Id);
    }
}
