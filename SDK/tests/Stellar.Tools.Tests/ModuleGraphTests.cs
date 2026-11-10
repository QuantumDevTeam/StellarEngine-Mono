using Stellar.Kernel.Configuration.Manifest;
using Stellar.Kernel.Modules;
using Stellar.Tools.Environment;
using Stellar.Tools.Manifests;
using Stellar.Tools.Modules;

namespace Stellar.Tools.Tests;

public class ModuleGraphTests
{
    private static ModuleDescriptor Module(string id, ModuleLevel level, params string[] dependencies)
    {
        return new ModuleDescriptor { Id = id, Version = "1.0.0", Level = level, Dependencies = dependencies };
    }

    [Fact]
    public void Analyze_OrdersDependenciesFirst()
    {
        var result = ModuleGraph.Analyze(
        [
            Module("Graphic", ModuleLevel.HighLevel, "Logging", "Core"),
            Module("Logging", ModuleLevel.CoreModule, "Core"),
            Module("Core", ModuleLevel.Core)
        ]);

        result.IsValid.Should().BeTrue();
        result.LoadOrder.Select(module => module.Id).Should().Equal("Core", "Logging", "Graphic");
    }

    [Fact]
    public void Analyze_IsDeterministic_RegardlessOfManifestOrder()
    {
        var modules = new[]
            { Module("B", ModuleLevel.HighLevel), Module("A", ModuleLevel.HighLevel), Module("C", ModuleLevel.Core) };

        var forward = ModuleGraph.Analyze(modules).LoadOrder.Select(module => module.Id);
        var backward = ModuleGraph.Analyze(((IEnumerable<ModuleDescriptor>)modules).Reverse()).LoadOrder
            .Select(module => module.Id);

        var expectation = forward as string[] ?? forward.ToArray();
        expectation.Should().Equal("C", "A", "B");
        backward.Should().Equal(expectation);
    }

    [Fact]
    public void Analyze_RejectsUnknownDependency()
    {
        var result = ModuleGraph.Analyze([Module("A", ModuleLevel.HighLevel, "Missing")]);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Missing");
        result.LoadOrder.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_RejectsDuplicate()
    {
        var result = ModuleGraph.Analyze([Module("A", ModuleLevel.Core), Module("A", ModuleLevel.Core)]);

        result.Errors.Should().ContainSingle().Which.Should().Contain("twice");
    }

    [Theory]
    [InlineData(ModuleLevel.Core, ModuleLevel.HighLevel)]
    [InlineData(ModuleLevel.CoreModule, ModuleLevel.CoreModule)]
    public void Analyze_RejectsDependencyOnSameOrHigherLevel(ModuleLevel level, ModuleLevel dependencyLevel)
    {
        var result = ModuleGraph.Analyze([Module("A", level, "B"), Module("B", dependencyLevel)]);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Analyze_AllowsRuntimeToReachUp()
    {
        var result = ModuleGraph.Analyze(
        [
            Module("Runtime", ModuleLevel.Runtime, "Graphic"),
            Module("Graphic", ModuleLevel.HighLevel)
        ]);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Analyze_ReportsCycle()
    {
        var result = ModuleGraph.Analyze(
        [
            Module("A", ModuleLevel.Runtime, "B"),
            Module("B", ModuleLevel.Runtime, "A")
        ]);

        result.Errors.Should().ContainSingle().Which.Should().Contain("cycle");
    }
}

public class ManifestTests
{
    [Fact]
    public void Store_RoundTripsManifest()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "manifests", "engine.manifest.json");

        EngineManifestStore.Save(path, new EngineManifest
        {
            OrchesterVersion = "1.2.3",
            Modules = [new ModuleOverride { Id = "Stellar.Graphic", Level = ModuleLevel.HighLevel, Enabled = false }]
        });

        var loaded = EngineManifestStore.Load(path);

        loaded.OrchesterVersion.Should().Be("1.2.3");
        loaded.Discovery.Directories.Should().Equal("modules");
        loaded.Modules.Should().ContainSingle().Which.Level.Should().Be(ModuleLevel.HighLevel);
        File.ReadAllText(path).Should().Contain("\"HighLevel\"").And.NotContain("\"Path\"");
    }

    [Fact]
    public void Load_RejectsUnknownFormatVersion()
    {
        using var directory = new TempDirectory();
        var path = directory.Write("manifest.json", """{ "SchemaVersion": 99 }""");

        var action = () => EngineManifestStore.Load(path);

        action.Should().Throw<Diagnostics.ToolException>().WithMessage("*99*");
    }

    [Fact]
    public void Load_ReportsMalformedJson()
    {
        using var directory = new TempDirectory();
        var path = directory.Write("manifest.json", "{ not json");

        var action = () => EngineManifestStore.Load(path);

        action.Should().Throw<Diagnostics.ToolException>().WithMessage($"*{Path.GetFileName(path)}*");
    }
}
