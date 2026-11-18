using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Projects;
using Stellar.Tools.Solutions;

namespace Stellar.Tools.Tests;

internal sealed class FakeReferences(Dictionary<string, string[]> references)
    : IProjectReferenceReader
{
    public List<string> Read { get; } = [];

    IReadOnlyList<string> IProjectReferenceReader.Read(string projectPath)
    {
        Read.Add(Path.GetFileName(projectPath));
        return references.TryGetValue(Path.GetFileName(projectPath), out var found)
            ? found
            : throw new ToolException("not evaluated");
    }
}

public class SolutionFileTests
{
    [Fact]
    public void ASolution_HasItsFolders_AndTheEngineIsNotBuiltInRelease()
    {
        var directory = Path.Combine(Path.GetTempPath(), "game");
        var game = new SolutionProject(Path.Combine(directory, "Game", "Game.csproj"));
        var core = new SolutionProject(Path.Combine(directory, "..", "engine", "Core.csproj"), "Engine", BuildsInRelease: false);

        var text = SolutionFile.Render(directory, [game, core]);

        text.Should().Contain("\"Game\", \"Game\\Game.csproj\"");
        text.Should().Contain("\"Core\", \"..\\engine\\Core.csproj\"");
        text.Should().Contain("\"Engine\", \"Engine\"");
        text.Should().Contain("GlobalSection(NestedProjects)");
        text.Split('\n').Count(line => line.Contains(".Release|Any CPU.Build.0")).Should().Be(1);
        SolutionFile.Render(directory, [game, core]).Should().Be(text, "the same solution is written the same way");
    }

    [Fact]
    public void APathOnAnotherDrive_StaysWhole()
    {
        if (!OperatingSystem.IsWindows()) return;

        SolutionFile.Relative(@"C:\games\demo", @"F:\engine\Core.csproj").Should().Be(@"F:\engine\Core.csproj");
    }
}

public class SolutionGeneratorTests
{
    [Fact]
    public void TheEngine_GoesIntoItsFolders_AsTheBuildReferencesIt()
    {
        using var directory = new TempDirectory();
        var root = Path.Combine(directory.Path, "stellar");
        var game = directory.Write("demo/Demo/Demo.csproj");
        var kernel = directory.Write("stellar/Kernel/Stellar.Kernel.csproj");
        var core = directory.Write("stellar/Core/Stellar.Core.csproj");
        var module = directory.Write("stellar/modules/Stellar.FileSystem/Stellar.FileSystem.csproj");
        var shared = directory.Write("demo/Shared/Shared.csproj");
        var references = new FakeReferences(new Dictionary<string, string[]>
        {
            ["Demo.csproj"] = [core, module, shared],
            ["Stellar.Core.csproj"] = [kernel],
            ["Stellar.FileSystem.csproj"] = [core, kernel],
            ["Shared.csproj"] = [],
            ["Stellar.Kernel.csproj"] = []
        });

        var solution = Path.Combine(directory.Path, "demo", "Demo.sln");
        var projects = new SolutionGenerator(root, references, NullToolLogger.Instance)
            .Generate(solution, [new SolutionProject(game)]);

        projects.Select(project => (Path.GetFileNameWithoutExtension(project.Path), project.Folder)).Should().BeEquivalentTo(
        [
            ("Demo", (string?)null), ("Stellar.Core", "Engine"), ("Stellar.FileSystem", "Modules"), ("Shared", null),
            ("Stellar.Kernel", "Engine")
        ]);
        references.Read.Should().OnlyHaveUniqueItems();
        File.ReadAllText(solution).Should().Contain("\"Modules\", \"Modules\"");
    }

    [Fact]
    public void AProjectMSBuildCannotRead_LeavesTheEngineOut()
    {
        using var directory = new TempDirectory();
        var game = directory.Write("demo/Demo/Demo.csproj");

        var projects = new SolutionGenerator(Path.Combine(directory.Path, "stellar"), new FakeReferences([]), NullToolLogger.Instance)
            .Generate(Path.Combine(directory.Path, "demo", "Demo.sln"), [new SolutionProject(game)]);

        projects.Should().ContainSingle();
    }
}

public class SolutionScaffolderTests
{
    private static readonly StellarInstallation Home = new(Path.Combine(Path.GetTempPath(), "home"));

    private static ProjectScaffolder Scaffolder(TempDirectory directory)
    {
        directory.Write("templates/Game/global.json", "{}");
        directory.Write("templates/Game/Directory.Build.props", "<Project/>");
        directory.Write("templates/Game/__ProjectName__.stellar.solution", "{ \"Run\": { \"Default\": \"{{ProjectName}}\" } }");
        directory.Write("templates/Game/__ProjectName__/__ProjectName__.csproj", "<StellarSdkUsage>ExecutableModule</StellarSdkUsage>");
        directory.Write("templates/Game/__ProjectName__/EntryPoint.cs", "namespace {{ProjectName}};");
        return new ProjectScaffolder(Path.Combine(directory.Path, "templates"), NullToolLogger.Instance);
    }

    [Fact]
    public void AProject_LivesInItsFolder_UnderItsSolution()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "Demo");

        Scaffolder(directory).Create("Game", "Demo", output, Home, references: new FakeReferences(new() { ["Demo.csproj"] = [] }));

        File.Exists(Path.Combine(output, "Demo.sln")).Should().BeTrue();
        File.Exists(Path.Combine(output, "Demo.stellar.solution")).Should().BeTrue();
        File.Exists(Path.Combine(output, "global.json")).Should().BeTrue();
        File.Exists(Path.Combine(output, "Demo", "Demo.csproj")).Should().BeTrue();
        File.ReadAllText(Path.Combine(output, "Demo.sln")).Should().Contain("Demo\\Demo.csproj");
    }

    [Fact]
    public void AFlatProject_HasNoSolution()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "Demo");

        Scaffolder(directory).Create("Game", "Demo", output, Home, flat: true);

        Directory.GetFileSystemEntries(output).Select(Path.GetFileName).Should()
            .BeEquivalentTo(["global.json", "Demo.csproj", "EntryPoint.cs"]);
    }

    [Fact]
    public void Sync_WritesTheProjectsOfTheSolution_AndTheEngineTheyReference()
    {
        using var directory = new TempDirectory();
        directory.Write("Demo.stellar.solution", "{ \"Engine\": \"Sources\" }");
        var game = Path.GetFullPath(directory.Write("Demo/Demo.csproj"));
        var tools = Path.GetFullPath(directory.Write("Tools/Tools.csproj"));
        directory.Write("Demo/bin/Debug/Copy.csproj");
        var core = Path.GetFullPath(directory.Write("stellar/Core/Stellar.Core.csproj"));
        var references = new FakeReferences(new()
        {
            ["Demo.csproj"] = [core], ["Tools.csproj"] = [], ["Stellar.Core.csproj"] = []
        });

        var solution = StellarSolution.Find(Path.Combine(directory.Path, "Demo"))!;
        var path = solution.Sync(new SolutionGenerator(Path.Combine(directory.Path, "stellar"), references, NullToolLogger.Instance));

        solution.File.Engine.Should().Be("Sources");
        path.Should().Be(Path.Combine(directory.Path, "Demo.sln"));
        var text = File.ReadAllText(path);
        text.Should().Contain("Demo\\Demo.csproj").And.Contain("Tools\\Tools.csproj").And.NotContain("Copy.csproj");
        references.Read.Should().Contain("Stellar.Core.csproj");
        _ = (game, tools);
    }
}

public class PackageCacheTests
{
    [Fact]
    public void APackageMadeAgain_TakesItsOldCopyOutOfTheCache_AndLeavesTheFreshOnes()
    {
        using var directory = new TempDirectory();
        var feed = Path.Combine(directory.Path, "packages");
        var cache = Path.Combine(directory.Path, "cache");
        var fresh = directory.Write("packages/Stellar.Core.0.9.0.nupkg");
        var kept = directory.Write("packages/Stellar.Kernel.0.9.0.nupkg");
        var oldCopy = directory.Write("cache/stellar.core/0.9.0/.nupkg.metadata");
        var freshCopy = directory.Write("cache/stellar.kernel/0.9.0/.nupkg.metadata");
        File.SetLastWriteTimeUtc(oldCopy, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(fresh, DateTime.UtcNow);
        File.SetLastWriteTimeUtc(kept, DateTime.UtcNow.AddHours(-2));
        File.SetLastWriteTimeUtc(freshCopy, DateTime.UtcNow.AddHours(-1));

        var removed = Stellar.Tools.Installation.PackageCache.RefreshStale(feed, cache, NullToolLogger.Instance);

        removed.Should().ContainSingle().Which.Should().EndWith(Path.Combine("stellar.core", "0.9.0"));
        Directory.Exists(Path.Combine(cache, "stellar.kernel", "0.9.0")).Should().BeTrue();
    }
}

public class RunPlannerTests
{
    private const string Executable = "<Project><PropertyGroup><StellarSdkUsage>ExecutableModule</StellarSdkUsage></PropertyGroup></Project>";

    [Fact]
    public void TheOnlyExecutable_RunsWithoutAName_FromAnywhereInTheSolution()
    {
        using var directory = new TempDirectory();
        directory.Write("Demo.stellar.solution", "{}");
        var game = Path.GetFullPath(directory.Write("Demo/Demo.csproj", Executable));
        directory.Write("Shared/Shared.csproj", "<Project/>");

        var plan = RunPlanner.Plan(Path.Combine(directory.Path, "Shared"), arguments: ["--level", "2"]);

        plan.Projects.Should().Equal(game);
        plan.Builds.Should().BeEmpty();
        plan.Runs.Single().Should().Equal("run", "--project", game, "-c", "Debug", "--", "--level", "2");
    }

    [Fact]
    public void AProfile_RunsItsProjectsAtOnce_AfterBuildingThemInTurn()
    {
        using var directory = new TempDirectory();
        directory.Write("Demo.stellar.solution", """
            {
              "Run": {
                "Default": "Network",
                "Profiles": {
                  "Network": {
                    "Projects": ["Server", "Client/Client.csproj"],
                    "Configuration": "Release",
                    "Arguments": ["--port", "7777"],
                    "Environment": { "STELLAR_LOG": "verbose" },
                    "Properties": { "StellarNativeWrapper": "Managed" }
                  }
                }
              }
            }
            """);
        var server = Path.GetFullPath(directory.Write("Server/Server.csproj", Executable));
        var client = Path.GetFullPath(directory.Write("Client/Client.csproj", Executable));

        var plan = RunPlanner.Plan(directory.Path);

        plan.Name.Should().Be("Network");
        plan.Projects.Should().Equal(server, client);
        plan.Environment.Should().ContainKey("STELLAR_LOG");
        plan.Builds.Should().HaveCount(2);
        plan.Builds[0].Should().Equal("build", server, "-c", "Release", "-p:StellarNativeWrapper=Managed");
        plan.Runs.Should().AllSatisfy(run => run.Should().Contain("--no-build").And.ContainInOrder("--", "--port", "7777"));
    }

    [Fact]
    public void Several_Executables_AndNoDefault_AskForAName()
    {
        using var directory = new TempDirectory();
        directory.Write("Demo.stellar.solution", "{}");
        directory.Write("Game/Game.csproj", Executable);
        directory.Write("Editor/Editor.csproj", Executable);

        var unnamed = () => RunPlanner.Plan(directory.Path);
        var unknown = () => RunPlanner.Plan(directory.Path, "Nope");

        unnamed.Should().Throw<ToolException>().WithMessage("*Editor*Game*");
        unknown.Should().Throw<ToolException>().WithMessage("*Nope*");
        RunPlanner.Plan(directory.Path, "editor").Name.Should().Be("Editor");
    }

    [Fact]
    public void WithoutASolution_TheProjectOfTheDirectoryRuns()
    {
        using var directory = new TempDirectory();
        var game = Path.GetFullPath(directory.Write("Game.csproj", Executable));

        RunPlanner.Plan(directory.Path, configuration: "Release", noBuild: true).Runs.Single().Should()
            .Equal("run", "--project", game, "-c", "Release", "--no-build");
    }
}
