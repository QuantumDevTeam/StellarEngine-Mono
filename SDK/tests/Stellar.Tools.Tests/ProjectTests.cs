using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Modules;
using Stellar.Runtime.Configuration;
using Stellar.Kernel.Configuration.Project.Assets;
using Stellar.Kernel.Configuration.Project.Localization;
using Stellar.Tools.Build;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Installation;
using Stellar.Tools.Projects;

namespace Stellar.Tools.Tests;

public class ProjectFileResolverTests
{
    private static StellarProject Project(TempDirectory directory, string json)
    {
        return StellarProject.Load(directory.Write("Demo.stellar.project", json));
    }

    [Fact]
    public void Resolve_SplitsAssets_EmbeddedWinsOverInclude()
    {
        using var directory = new TempDirectory();
        directory.Write("Assets/a.txt");
        directory.Write("Assets/b.txt");
        directory.Write("Assets/c.tmp");

        var files = ProjectFileResolver.Resolve(Project(directory, """
                                                                   { "Project": { "Assets": {
                                                                       "Include": ["Assets/**"], "Exclude": ["**/*.tmp"], "Embedded": ["Assets/b.txt"]
                                                                   } } }
                                                                   """));

        files.ExternalAssets.Select(file => file.RelativePath).Should().Equal("Assets/a.txt");
        files.EmbeddedAssets.Select(file => file.RelativePath).Should().Equal("Assets/b.txt");
    }

    [Fact]
    public void Resolve_WorksWithPartialSection()
    {
        // Regression: a section missing one list used to be skipped entirely
        using var directory = new TempDirectory();
        directory.Write("Assets/a.txt");

        var files = ProjectFileResolver.Resolve(Project(directory,
            """{ "Project": { "Assets": { "Include": ["Assets/**"] } } }"""));

        files.ExternalAssets.Should().ContainSingle();
    }

    [Fact]
    public void Resolve_NeverPicksBuildOutput()
    {
        using var directory = new TempDirectory();
        directory.Write("bin/Debug/a.txt");
        directory.Write("obj/a.txt");
        directory.Write("a.txt");

        var files = ProjectFileResolver.Resolve(Project(directory,
            """{ "Project": { "Assets": { "Include": ["**/*.txt"] } } }"""));

        files.ExternalAssets.Select(file => file.RelativePath).Should().Equal("a.txt");
    }

    [Fact]
    public void Resolve_DetectsCulture()
    {
        using var directory = new TempDirectory();
        directory.Write("Loc/ru.stellar.loc.index");

        var files = ProjectFileResolver.Resolve(Project(directory, """
                                                                   { "Project": { "Localizations": {
                                                                       "DefaultCulture": "ru", "Cultures": ["ru"], "IndexFiles": ["Loc/*"]
                                                                   } } }
                                                                   """));

        files.LocalizationFiles.Should().ContainSingle().Which.Culture.Should().Be("ru");
        files.DefaultCulture.Should().Be("ru");
    }

    [Fact]
    public void Resolve_RejectsDefaultCultureOutsideTheList()
    {
        using var directory = new TempDirectory();

        var action = () => ProjectFileResolver.Resolve(Project(directory, """
                                                                          { "Project": { "Localizations": { "DefaultCulture": "ru", "Cultures": ["en"] } } }
                                                                          """));

        action.Should().Throw<ToolException>().WithMessage("*ru*");
    }

    [Theory]
    [InlineData("""{ "Project": { "StellarEntryPoint": "default" } }""", "Demo.EntryPoint")]
    [InlineData("""{ "Project": { } }""", "Demo.EntryPoint")]
    [InlineData("""{ }""", "Demo.EntryPoint")]
    [InlineData("""{ "Project": { "StellarEntryPoint": "Game.Main" } }""", "Game.Main")]
    public void ResolveEntryPoint_FollowsTheConvention(string json, string expected)
    {
        using var directory = new TempDirectory();

        Project(directory, json).ResolveEntryPoint("Demo").Should().Be(expected);
    }

    [Fact]
    public void Project_NameComesFromTheConfigurationFile()
    {
        using var directory = new TempDirectory();

        Project(directory, "{ }").Name.Should().Be("Demo");
    }

    [Fact]
    public void Find_RequiresExactlyOneProject()
    {
        using var directory = new TempDirectory();

        var action = () => StellarProject.Find(directory.Path);
        action.Should().Throw<ToolException>();

        directory.Write("A.stellar.project", "{ }");
        StellarProject.Find(directory.Path).Name.Should().Be("A");

        directory.Write("B.stellar.project", "{ }");
        action.Should().Throw<ToolException>();
    }
}

public class RuntimeConfigurationTests
{
    [Fact]
    public void WhatToolsWrites_TheRuntimeReads()
    {
        using var directory = new TempDirectory();
        directory.Write("Assets/a.txt");
        directory.Write("Assets/b.txt");
        directory.Write("Loc/en.stellar.loc.index");

        var project = StellarProject.Load(directory.Write("Demo.stellar.project", """
            {
              "Project": {
                "Assets": { "Include": ["Assets/a.txt"], "Embedded": ["Assets/b.txt"] },
                "Localizations": { "DefaultCulture": "en", "Cultures": ["en"], "IndexFiles": ["Loc/*"] }
              },
              "Runtime": { "Title": "Demo", "Size": { "Width": 800 } }
            }
            """));

        var written = RuntimeConfigurationBuilder.Build(project, ProjectFileResolver.Resolve(project),
            new RuntimeConfigurationInput
            {
                AssemblyName = "Demo", CompanyName = "Quantum", Version = "1.0.0",
                OrchesterVersion = "0.7.0", EngineVersion = "0.7.0",
                Modules = [new RuntimeModuleData { Id = "Stellar.Core", Level = ModuleLevel.Core, Version = "0.7.0" }]
            });

        var read = RuntimeConfigurationReader.Read(StellarJson.Serialize(written));

        read.ProjectName.Should().Be("Demo");
        read.CompanyName.Should().Be("Quantum");
        read.EntryPoint.Should().Be("Demo.EntryPoint");
        read.Settings["Title"].Should().Be("Demo");
        read.Settings["Size"].Should().Be("""{"Width":800}""");

        var assets = read.Components.OfType<AssetsComponent>().Single();
        assets.ExternalAssets.Should().ContainSingle().Which.Path.Should().Be("Assets/a.txt");
        assets.EmbeddedAssets.Should().ContainSingle().Which.Path.Should().Be("Demo:Assets/b.txt");

        var localization = read.Components.OfType<LocalizationComponent>().Single();
        localization.DefaultCulture.Should().Be("en");
        localization.LocalizationIndexFiles.Should().ContainSingle().Which.Culture.Should().Be("en");

        var modules = read.Components.OfType<ModulesComponent>().Single();
        modules.Modules.Should().ContainSingle().Which.Level.Should().Be(ModuleLevel.Core);
    }

    [Fact]
    public void Reader_RejectsUnknownFormatVersion()
    {
        var action = () => RuntimeConfigurationReader.Read("""{ "SchemaVersion": 99 }""");

        action.Should().Throw<InvalidDataException>().WithMessage("*99*");
    }

    [Fact]
    public void Reader_SkipsUnknownComponents()
    {
        var read = RuntimeConfigurationReader.Read(
            """{ "SchemaVersion": 2, "Components": [ { "Kind": "FromTheFuture" } ] }""");

        read.Components.Should().BeEmpty();
    }

    [Fact]
    public void Launcher_HandsTheApplicationToTheRuntime()
    {
        var source = LauncherGenerator.Generate(new LauncherInfo { Application = "Game", Company = "Quantum \"Dev\"" });

        source.Should().Contain("global::Stellar.Runtime.Hosting.StellarRuntime.Run(\"Game\", args)");
        source.Should().Contain("static int Main");
        source.Should().Contain("AssemblyCompany(\"Quantum \\\"Dev\\\"\")");
        source.Should().NotContain("AssemblyCopyright");
    }

    [Fact]
    public void LauncherDependencyFile_ListsTheHostOnly()
    {
        var json = LauncherDependencyFile.Render(".NETCoreApp,Version=v9.0",
            new LauncherAssembly("Game.Launcher", "1.0.0"),
            [new LauncherAssembly("Stellar.Runtime", "0.7.0"), new LauncherAssembly("Stellar.Kernel", "0.7.0")]);

        var document = Newtonsoft.Json.Linq.JObject.Parse(json);
        document["runtimeTarget"]!["name"]!.ToString().Should().Be(".NETCoreApp,Version=v9.0");

        var target = (Newtonsoft.Json.Linq.JObject)document["targets"]![".NETCoreApp,Version=v9.0"]!;
        target.Properties().Select(property => property.Name).Should()
            .BeEquivalentTo("Game.Launcher/1.0.0", "Stellar.Runtime/0.7.0", "Stellar.Kernel/0.7.0");
        target["Game.Launcher/1.0.0"]!["dependencies"]!["Stellar.Runtime"]!.ToString().Should().Be("0.7.0");
        target["Stellar.Kernel/0.7.0"]!["runtime"]!["Stellar.Kernel.dll"].Should().NotBeNull();
        document["libraries"]!["Stellar.Runtime/0.7.0"]!["type"]!.ToString().Should().Be("project");
    }
}

public class RuntimeModulePlannerTests
{
    private static CatalogModule Module(string id, ModuleLevel level, params string[] dependencies)
    {
        return new CatalogModule(id, level, "1.0.0", dependencies);
    }

    private static readonly CatalogModule[] Catalog =
    [
        Module("Stellar.Kernel", ModuleLevel.Kernel),
        Module("Stellar.Core", ModuleLevel.Core, "Stellar.Kernel"),
        Module("Stellar.Runtime", ModuleLevel.Runtime, "Stellar.Kernel"),
        Module("Stellar.Logging", ModuleLevel.CoreModule),
        Module("Stellar.Graphic", ModuleLevel.HighLevel, "Stellar.Logging"),
        Module("Stellar.Audio", ModuleLevel.HighLevel, "Stellar.Logging"),
        Module("Stellar.Unused", ModuleLevel.HighLevel)
    ];

    [Fact]
    public void Plan_TakesTheDependenciesOfTheDependencies_LowerLevelsFirst()
    {
        var plan = RuntimeModulePlanner.Plan(Catalog, ["Stellar.Graphic", "Stellar.Audio", "Stellar.Core"],
            out var unknown);

        plan.Select(module => module.Id).Should().Equal(
            "Stellar.Kernel", "Stellar.Core", "Stellar.Logging", "Stellar.Audio", "Stellar.Graphic");
        plan.Single(module => module.Id == "Stellar.Graphic").Dependencies.Should().Equal("Stellar.Logging");
        unknown.Should().BeEmpty();
    }

    [Fact]
    public void Plan_ReportsUnknownModules()
    {
        RuntimeModulePlanner.Plan(Catalog, ["Stellar.Graphic", "Stellar.Missing"], out var unknown);

        unknown.Should().Equal("Stellar.Missing");
    }

    [Fact]
    public void Plan_RejectsCycles()
    {
        var action = () => RuntimeModulePlanner.Plan(
            [Module("A", ModuleLevel.HighLevel, "B"), Module("B", ModuleLevel.HighLevel, "A")], ["A"], out _);

        action.Should().Throw<InvalidOperationException>().WithMessage("*cycle*A, B*");
    }
}

public class ScaffolderTests
{
    private static readonly StellarInstallation Home = new(Path.Combine(Path.GetTempPath(), "home"));

    private static ProjectScaffolder Scaffolder(TempDirectory directory)
    {
        directory.Write("templates/Game/__ProjectName__.csproj", "<Project Sdk=\"{{SdkVersion}}\"/>");
        directory.Write("templates/Game/Source/{{Untouched}}.cs", "namespace {{ProjectName}};");

        return new ProjectScaffolder(Path.Combine(directory.Path, "templates"), NullToolLogger.Instance);
    }

    [Fact]
    public void Create_FillsPlaceholders()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "out");

        Scaffolder(directory).Create("Game", "Demo", output, Home);

        File.ReadAllText(Path.Combine(output, "Demo.csproj")).Should().Contain(StellarVersions.Sdk);
        File.ReadAllText(Path.Combine(output, "Source", "{{Untouched}}.cs")).Should().Be("namespace Demo;");
    }

    [Fact]
    public void ListTemplates_ShowsDirectories()
    {
        using var directory = new TempDirectory();

        Scaffolder(directory).ListTemplates().Should().Equal("Game");
    }

    [Theory]
    [InlineData("1Demo")]
    [InlineData("De mo")]
    [InlineData("../Demo")]
    public void Create_RejectsInvalidName(string name)
    {
        using var directory = new TempDirectory();

        var action = () => Scaffolder(directory).Create("Game", name, Path.Combine(directory.Path, "out"), Home);

        action.Should().Throw<ToolException>();
    }

    [Fact]
    public void Create_RejectsUnknownTemplate()
    {
        using var directory = new TempDirectory();

        var action = () => Scaffolder(directory).Create("Nope", "Demo", Path.Combine(directory.Path, "out"), Home);

        action.Should().Throw<ToolException>().WithMessage("*Game*");
    }

    [Fact]
    public void Create_NeverWritesOverAnExistingProject()
    {
        using var directory = new TempDirectory();
        var keep = directory.Write("out/keep.txt", "precious");

        var action = () => Scaffolder(directory).Create("Game", "Demo", Path.GetDirectoryName(keep)!, Home);

        action.Should().Throw<ToolException>();
        File.ReadAllText(keep).Should().Be("precious");
    }
}

public class InstallerTests
{
    [Fact]
    public void Install_RefusesADirectoryThatIsNotTheCore()
    {
        using var directory = new TempDirectory();

        var action = () =>
            EngineInstaller.Install(new StellarInstallation(directory.Path), NullToolLogger.Instance, false);

        action.Should().Throw<ToolException>().WithMessage("*not a checkout of StellarEngine-Core*");
    }

    [Fact]
    public void Install_CreatesTheLayoutInPlace_AndKeepsWhatExists()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);

        EngineInstaller.Install(installation, NullToolLogger.Instance, false);

        foreach (var path in new[]
                 {
                     installation.Modules, installation.Packages, installation.Tools, installation.Manifests,
                     installation.Config, installation.Projects
                 })
            Directory.Exists(path).Should().BeTrue(path);

        File.Exists(installation.ModuleCatalog).Should().BeTrue();
        File.ReadAllText(Path.Combine(installation.Modules, "Directory.Build.props")).Should().Be("isolation");

        // A second run keeps the manifest and the files of the template the user changed
        var marked = File.ReadAllText(installation.EngineManifest).Replace("\"Modules\"", " \"Modules\"");
        File.WriteAllText(installation.EngineManifest, marked);
        File.WriteAllText(Path.Combine(installation.Modules, "Directory.Build.props"), "edited");

        EngineInstaller.Install(installation, NullToolLogger.Instance, false);

        File.ReadAllText(installation.EngineManifest).Should().Be(marked);
        File.ReadAllText(Path.Combine(installation.Modules, "Directory.Build.props")).Should().Be("edited");
    }

    [Fact]
    public void FileWriter_LeavesIdenticalFileUntouched()
    {
        using var directory = new TempDirectory();
        var path = directory.Write("file.txt", "same");
        var before = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, before);

        FileWriter.WriteAllText(path, "same").Should().BeFalse();
        File.GetLastWriteTimeUtc(path).Should().Be(before);

        FileWriter.WriteAllText(path, "different").Should().BeTrue();
    }
}

public class StellarInstallationTests
{
    [Fact]
    public void Layout_MatchesTheInstallationTree()
    {
        var root = Path.Combine(Path.GetTempPath(), "StellarEngine");
        var installation = new StellarInstallation(root);

        installation.Packages.Should().Be(Path.Combine(root, "packages"));
        installation.Modules.Should().Be(Path.Combine(root, "modules"));
        installation.Templates.Should().Be(Path.Combine(root, "Shared", "Templates"));
        installation.EngineManifest.Should().Be(Path.Combine(root, "manifests", "engine.manifest.json"));
        installation.ResourceCompilers.Should().Be(Path.Combine(root, "tools", "compilers"));
    }

    [Fact]
    public void Resolve_TakesRelativePathsFromTheRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "StellarEngine");
        var installation = new StellarInstallation(root);

        installation.Resolve("modules/A").Should().Be(Path.Combine(root, "modules", "A"));
        installation.Resolve(Path.Combine(Path.GetTempPath(), "elsewhere")).Should()
            .Be(Path.Combine(Path.GetTempPath(), "elsewhere"));
    }

    [Fact]
    public void Versions_AreBakedIn()
    {
        StellarVersions.Orchester.Should().NotBeNullOrEmpty();
        StellarVersions.Core.Should().NotBeNullOrEmpty();
        StellarVersions.NativeAbi.Should().NotBeNullOrEmpty();
    }
}
