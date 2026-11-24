using Stellar.Kernel.Configuration.Manifest;
using Stellar.Kernel.Modules;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Modules;
using Stellar.Tools.Solutions;

namespace Stellar.Tools.Tests;

public class SolutionModulesTests
{
    private static readonly ResolvedModule[] Installed =
    [
        new(new ModuleDescriptor { Id = "Stellar.FileSystem", Level = ModuleLevel.CoreModule, Version = "0.9.0" },
            null, ModuleSource.Discovered, ModuleLink.Default)
    ];

    private static StellarSolution Solution(TempDirectory directory, string json = "{}")
    {
        directory.Write("Game.stellar.solution", json);
        return StellarSolution.Find(directory.Path)!;
    }

    private static void Module(TempDirectory directory, string folder, string id, string json = "")
    {
        var body = json.Length == 0 ? string.Empty : ", " + json;
        directory.Write($"{folder}/{id}.stellar.module", $$"""{ "Id": "{{id}}", "Level": "EndPoint"{{body}} }""");
        directory.Write($"{folder}/{id}.csproj", "<Project/>");
    }

    [Fact]
    public void AModuleBesideTheGame_IsFound_AsACustomModuleTakenFromSources()
    {
        using var directory = new TempDirectory();
        directory.Write("Game/Game.csproj", "<Project/>");
        Module(directory, "GameLib", "GameLib");
        directory.Write("GameLib/bin/Debug/Copy.stellar.module", """{ "Id": "Copy" }""");

        var module = SolutionModules.Resolve(Solution(directory), Installed).Should().ContainSingle().Subject;

        module.Id.Should().Be("GameLib");
        module.Source.Should().Be(ModuleSource.Solution);
        module.Link.Should().Be(ModuleLink.Sources);
        module.Descriptor.Version.Should().Be(SolutionModules.DefaultVersion);
        module.ProjectPath.Should().EndWith(Path.Combine("GameLib", "GameLib.csproj"));
    }

    [Fact]
    public void AModuleOutsideTheDirectory_IsFound_WhenTheSolutionListsIt()
    {
        using var directory = new TempDirectory();
        directory.Write("game/Game.stellar.solution", """{ "Modules": ["../shared/Lib"] }""");
        Module(directory, "shared/Lib", "Lib");
        var solution = StellarSolution.Find(Path.Combine(directory.Path, "game"))!;

        SolutionModules.Resolve(solution, Installed).Select(module => module.Id).Should().Equal("Lib");
    }

    [Fact]
    public void AListedFolderWithoutADescriptor_IsAnError()
    {
        using var directory = new TempDirectory();
        var solution = Solution(directory, """{ "Modules": ["Nothing"] }""");
        Directory.CreateDirectory(Path.Combine(directory.Path, "Nothing"));

        var action = () => SolutionModules.Resolve(solution, Installed);

        action.Should().Throw<ToolException>().WithMessage("*Nothing*no module descriptor*");
    }

    [Fact]
    public void AModule_OfAnotherLevel_IsRefused()
    {
        using var directory = new TempDirectory();
        directory.Write("Lib/Lib.stellar.module", """{ "Id": "Lib", "Level": "HighLevel" }""");
        directory.Write("Lib/Lib.csproj", "<Project/>");

        var action = () => SolutionModules.Resolve(Solution(directory), Installed);

        action.Should().Throw<ToolException>().WithMessage("*Lib*EndPoint*HighLevel*");
    }

    [Fact]
    public void AModule_MayDependOnAModuleOfTheInstallation_AndTheCoreIsImplicit()
    {
        using var directory = new TempDirectory();
        Module(directory, "Lib", "Lib", """ "Dependencies": ["Stellar.FileSystem", "Stellar.Kernel", "Stellar.Core"] """);

        var module = SolutionModules.Resolve(Solution(directory), Installed).Single();

        module.Descriptor.Dependencies.Should().Equal("Stellar.FileSystem");
    }

    [Fact]
    public void AnUnknownDependency_AndAnIdOfTheInstallation_AreRefused()
    {
        using var unknown = new TempDirectory();
        Module(unknown, "Lib", "Lib", """ "Dependencies": ["Nope"] """);
        using var twice = new TempDirectory();
        Module(twice, "Lib", "Stellar.FileSystem");

        var first = () => SolutionModules.Resolve(Solution(unknown), Installed);
        var second = () => SolutionModules.Resolve(Solution(twice), Installed);

        first.Should().Throw<ToolException>().WithMessage("*unknown module Nope*");
        second.Should().Throw<ToolException>().WithMessage("*Stellar.FileSystem*twice*");
    }

    [Fact]
    public void Sync_WritesTheCatalog_AndRemovesItWhenTheModulesAreGone()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Installed(directory);
        directory.Write("game/Game.stellar.solution", "{}");
        Module(directory, "game/GameLib", "GameLib");
        var solution = StellarSolution.Find(Path.Combine(directory.Path, "game"))!;
        var catalog = SolutionModules.CatalogPath(solution.Directory);

        SolutionModules.Sync(solution, installation, NullToolLogger.Instance);

        var text = File.ReadAllText(catalog);
        text.Should().Contain("Include=\"GameLib\"").And.Contain("LinkMode=\"Sources\"")
            .And.Contain("Source=\"Solution\"").And.Contain("Level=\"EndPoint\"")
            .And.Contain("stellar solution sync").And.Contain("$(MSBuildThisFileFullPath)");

        File.Delete(Path.Combine(directory.Path, "game", "GameLib", "GameLib.stellar.module"));
        SolutionModules.Sync(solution, installation, NullToolLogger.Instance);

        File.Exists(catalog).Should().BeFalse();
    }
}
