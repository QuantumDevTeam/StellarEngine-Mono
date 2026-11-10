using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Stellar.Sdk.Analyzers.Tests;

/// <summary>
/// Runs an analyzer the way the SDK sets it up: build properties as global options, configuration files as
/// additional files, modules as references carrying their [AssemblyMetadata].
/// </summary>
internal static class AnalyzerHarness
{
    private static readonly ImmutableArray<MetadataReference> Runtime =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Where(path =>
            Path.GetFileName(path) is "System.Runtime.dll" or "System.Private.CoreLib.dll" or "netstandard.dll")
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToImmutableArray();

    /// <summary>
    /// Builds an assembly the way the SDK stamps a module.
    /// </summary>
    public static MetadataReference Module(string id, string level, string dependencies = "")
    {
        var source = $$"""
                       [assembly: System.Reflection.AssemblyMetadata("StellarModuleId", "{{id}}")]
                       [assembly: System.Reflection.AssemblyMetadata("StellarModuleLevel", "{{level}}")]
                       [assembly: System.Reflection.AssemblyMetadata("StellarModuleDependencies", "{{dependencies}}")]
                       namespace {{id}} { public static class Marker { } }
                       """;

        var compilation = CSharpCompilation.Create(id, [CSharpSyntaxTree.ParseText(source)], Runtime,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
            throw new InvalidOperationException(string.Join("\n", result.Diagnostics));

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    public static ImmutableArray<Diagnostic> Run(
        DiagnosticAnalyzer analyzer,
        IReadOnlyDictionary<string, string> properties,
        IEnumerable<MetadataReference>? references = null,
        string assemblyName = "Stellar.Subject",
        params (string Path, string Text)[] additionalFiles)
    {
        var compilation = CSharpCompilation.Create(assemblyName,
            [CSharpSyntaxTree.ParseText("namespace Subject { public static class Code { } }")],
            Runtime.AddRange(references ?? []),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var options = new AnalyzerOptions(
            additionalFiles.Select(file => (AdditionalText)new InMemoryText(file.Path, file.Text)).ToImmutableArray(),
            new GlobalOptionsProvider(properties.ToDictionary(pair => "build_property." + pair.Key,
                pair => pair.Value)));

        return compilation
            .WithAnalyzers([analyzer], options)
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter().GetResult();
    }

    private sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(text);
        }
    }

    private sealed class GlobalOptionsProvider(Dictionary<string, string> values) : AnalyzerConfigOptionsProvider
    {
        private readonly Options _global = new(values);

        public override AnalyzerConfigOptions GlobalOptions => _global;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return Options.Empty;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return Options.Empty;
        }

        private sealed class Options(Dictionary<string, string> values) : AnalyzerConfigOptions
        {
            public static readonly Options Empty = new([]);

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                return values.TryGetValue(key, out value);
            }
        }
    }
}
