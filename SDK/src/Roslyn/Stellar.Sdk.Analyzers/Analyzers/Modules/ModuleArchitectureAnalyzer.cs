using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Stellar.Sdk.Analyzers.Configuration;
using Stellar.Sdk.Analyzers.Diagnostics;
using Stellar.Sdk.Analyzers.Internal;

namespace Stellar.Sdk.Analyzers.Analyzers.Modules;

/// <summary>
/// Keeps the architecture of the engine: the level rule between modules, and the descriptor as the single
/// description of what a module depends on.
/// </summary>
/// <remarks>
/// The level and the dependencies of the project come from the SDK (CompilerVisibleProperty), the ones of the
/// referenced assemblies from the <c>[AssemblyMetadata("StellarModule…")]</c> the SDK stamps into every module.
/// Assemblies without it are not modules, and are not checked.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModuleArchitectureAnalyzer
    : DiagnosticAnalyzer
{
    private const string MetadataAttribute = "System.Reflection.AssemblyMetadataAttribute";
    private const string DescriptorExtension = ".stellar.module";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            StellarDiagnosticDescriptors.ModuleLevelViolation,
            StellarDiagnosticDescriptors.UndeclaredModuleDependency,
            StellarDiagnosticDescriptors.ModuleDescriptorMismatch);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(Analyze);
    }

    private static void Analyze(CompilationAnalysisContext context)
    {
        var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        string? Property(string name)
        {
            return options.TryGetValue("build_property." + name, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        var id = Property("StellarModuleId") ?? context.Compilation.AssemblyName ?? string.Empty;
        var isModule = string.Equals(Property("StellarIsModule"), "true", StringComparison.OrdinalIgnoreCase);
        var level = ModuleLevels.Parse(Property("StellarModuleLevel")) ??
                    ModuleLevels.FromUsage(Property("StellarSdkUsage"));

        if (level is null)
            return;

        var declared = new HashSet<string>(
            (Property("StellarModuleDependencies") ?? string.Empty)
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(name => name.Trim()),
            StringComparer.Ordinal);

        // Every referenced module, with what it says about itself
        var modules = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var reference in context.Compilation.References)
        {
            if (context.Compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                continue;

            var metadata = ReadMetadata(assembly);
            if (metadata.TryGetValue("StellarModuleId", out var dependencyId) &&
                !string.Equals(dependencyId, id, StringComparison.Ordinal))
                modules[dependencyId] = metadata;
        }

        // The compiler sees transitive references too. A module another referenced module depends on is not a
        // dependency of this project: only the direct ones are checked
        var transitive = new HashSet<string>(StringComparer.Ordinal);
        foreach (var metadata in modules.Values)
            AddDependencies(metadata, modules, transitive);

        foreach (var pair in modules.Where(pair => !transitive.Contains(pair.Key))
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var dependencyId = pair.Key;
            pair.Value.TryGetValue("StellarModuleLevel", out var dependencyLevelName);
            var dependencyLevel = ModuleLevels.Parse(dependencyLevelName);

            if (dependencyLevel is { } known && !ModuleLevels.MayDependOn(level.Value, known))
                context.ReportDiagnostic(Diagnostic.Create(
                    StellarDiagnosticDescriptors.ModuleLevelViolation,
                    Location.None,
                    id, ModuleLevels.Name(level.Value), dependencyId, ModuleLevels.Name(known)));

            if (isModule && !ModuleLevels.IsCore(dependencyId) && !declared.Contains(dependencyId))
                context.ReportDiagnostic(Diagnostic.Create(
                    StellarDiagnosticDescriptors.UndeclaredModuleDependency,
                    Location.None,
                    id, dependencyId));
        }

        if (isModule)
            CheckDescriptor(context, id);
    }

    private static void CheckDescriptor(CompilationAnalysisContext context, string id)
    {
        var descriptor = context.Options.AdditionalFiles.FirstOrDefault(file =>
            file.Path.EndsWith(DescriptorExtension, StringComparison.OrdinalIgnoreCase));
        var text = descriptor?.GetText(context.CancellationToken);
        if (descriptor is null || text is null)
            return;

        var describedId = MinimalJson.GetString(text.ToString(), "Id");
        if (describedId is null || string.Equals(describedId, id, StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            StellarDiagnosticDescriptors.ModuleDescriptorMismatch,
            JsonTextLocator.FindValueLocation(text, descriptor.Path, "Id"),
            describedId, id));
    }

    private static void AddDependencies(
        Dictionary<string, string> metadata,
        Dictionary<string, Dictionary<string, string>> modules,
        HashSet<string> found)
    {
        if (!metadata.TryGetValue("StellarModuleDependencies", out var list))
            return;

        foreach (var dependency in list.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(name => name.Trim()))
            if (found.Add(dependency) && modules.TryGetValue(dependency, out var next))
                AddDependencies(next, modules, found);
    }

    private static Dictionary<string, string> ReadMetadata(IAssemblySymbol assembly)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var attribute in assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != MetadataAttribute ||
                attribute.ConstructorArguments.Length != 2)
                continue;

            if (attribute.ConstructorArguments[0].Value is string key &&
                attribute.ConstructorArguments[1].Value is string value)
                values[key] = value;
        }

        return values;
    }
}
