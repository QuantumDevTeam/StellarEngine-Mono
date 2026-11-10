using Stellar.Sdk.Analyzers.Analyzers.Configuration;
using Stellar.Sdk.Analyzers.Analyzers.Modules;

namespace Stellar.Sdk.Analyzers.Tests;

public class ModuleArchitectureTests
{
    private static Dictionary<string, string> Module(string id, string level, string dependencies = "")
    {
        return new Dictionary<string, string>
        {
            ["StellarModuleId"] = id,
            ["StellarIsModule"] = "true",
            ["StellarModuleLevel"] = level,
            ["StellarModuleDependencies"] = dependencies
        };
    }

    private static IEnumerable<string> Ids(IEnumerable<Microsoft.CodeAnalysis.Diagnostic> diagnostics)
    {
        return diagnostics.Select(diagnostic => diagnostic.Id);
    }

    [Fact]
    public void ALowerModuleMustNotDependOnAHigherOne()
    {
        var diagnostics = AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(),
            Module("Stellar.Logging", "CoreModule", "Stellar.Graphic"),
            [AnalyzerHarness.Module("Stellar.Graphic", "HighLevel")]);

        Ids(diagnostics).Should().Equal("STLR0201");
        diagnostics[0].GetMessage().Should().Contain("Stellar.Logging").And.Contain("Stellar.Graphic");
    }

    [Theory]
    [InlineData("HighLevel", "CoreModule")]
    [InlineData("Runtime", "HighLevel")]
    [InlineData("EndPoint", "EndPoint")]
    [InlineData("CoreModule", "Core")]
    public void TheLevelRuleAllows(string level, string dependencyLevel)
    {
        var diagnostics = AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(),
            Module("Stellar.Subject", level, "Stellar.Other"),
            [AnalyzerHarness.Module("Stellar.Other", dependencyLevel)]);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void AProjectThatIsNotAModuleIsCheckedByItsKind()
    {
        var game = new Dictionary<string, string> { ["StellarSdkUsage"] = "EngineCoreModule" };

        var diagnostics = AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(), game,
            [AnalyzerHarness.Module("Stellar.Graphic", "HighLevel")]);

        // Not a module: the level rule holds, the descriptor rule does not apply
        Ids(diagnostics).Should().Equal("STLR0201");
    }

    [Fact]
    public void AModuleMustDeclareTheModulesItUses()
    {
        var undeclared = AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(),
            Module("Stellar.Graphic", "HighLevel"),
            [AnalyzerHarness.Module("Stellar.Logging", "CoreModule"), AnalyzerHarness.Module("Stellar.Core", "Core")]);

        var declared = AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(),
            Module("Stellar.Graphic", "HighLevel", "Stellar.Logging;Stellar.Other"),
            [AnalyzerHarness.Module("Stellar.Logging", "CoreModule")]);

        Ids(undeclared).Should().Equal("STLR0202");
        declared.Should().BeEmpty();
    }

    [Fact]
    public void OnlyDirectDependenciesAreChecked()
    {
        // Stellar.Graphic brings Stellar.Logging along: the project depends on Stellar.Graphic only
        var diagnostics = AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(),
            Module("Stellar.Editor", "EndPoint", "Stellar.Graphic"),
            [
                AnalyzerHarness.Module("Stellar.Graphic", "HighLevel", "Stellar.Logging"),
                AnalyzerHarness.Module("Stellar.Logging", "CoreModule")
            ]);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void TheDescriptorMustNameTheProject()
    {
        var diagnostics = AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(),
            Module("Stellar.Graphic", "HighLevel"),
            additionalFiles: ("C:/modules/Stellar.Graphic/Stellar.Graphic.stellar.module",
                """{ "Id": "Stellar.Graphics" }"""));

        Ids(diagnostics).Should().Equal("STLR0203");
    }

    [Fact]
    public void NothingIsCheckedWithoutALevel()
    {
        AnalyzerHarness.Run(new ModuleArchitectureAnalyzer(), new Dictionary<string, string>(),
            [AnalyzerHarness.Module("Stellar.Graphic", "HighLevel")]).Should().BeEmpty();
    }
}

public class ConfigurationFileTests
{
    [Theory]
    [InlineData("""{ "Project": { "StellarEntryPoint": "default" } }""")]
    [InlineData("""{ "Id": "Stellar.A", "Dependencies": [], "Level": "HighLevel", "Version": "1.0.0" }""")]
    [InlineData("""{ "Runtime": { "Value": 1.5e3, "Flag": true, "Nothing": null, "Text": "a \" b" } }""")]
    public void WellFormedFilesPass(string json)
    {
        AnalyzerHarness.Run(new ConfigurationFileAnalyzer(), new Dictionary<string, string>(),
            additionalFiles: ("Demo.stellar.project", json)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{ "Project": { "StellarEntryPoint": "default", } }""", 0)]
    [InlineData("{\n  \"Id\": \"Stellar.A\"\n  \"Level\": \"HighLevel\"\n}", 2)]
    [InlineData("{ \"Id\": \"Stellar.A }", 0)]
    public void TheMistakeIsPointedAt(string json, int line)
    {
        var diagnostics = AnalyzerHarness.Run(new ConfigurationFileAnalyzer(), new Dictionary<string, string>(),
            additionalFiles: ("Stellar.A.stellar.module", json));

        diagnostics.Should().ContainSingle().Which.Id.Should().Be("STLR0301");
        diagnostics[0].Location.GetLineSpan().StartLinePosition.Line.Should().Be(line);
    }

    [Fact]
    public void OtherFilesAreNotItsBusiness()
    {
        AnalyzerHarness.Run(new ConfigurationFileAnalyzer(), new Dictionary<string, string>(),
            additionalFiles: ("notes.json", "{ broken")).Should().BeEmpty();
    }
}
