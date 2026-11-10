using Stellar.Kernel.Configuration.Manifest;
using Stellar.Kernel.Modules;
using Stellar.Tools.Building;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Manifests;
using Stellar.Tools.Modules;

namespace Stellar.Tools.Tests;

/// <summary>
/// A throwaway checkout of the core, installed: just enough of it for the tools to accept it.
/// </summary>
internal static class FakeCore
{
    public static StellarInstallation Create(TempDirectory directory)
    {
        directory.Write("Shared/Build/Stellar.Versions.props", "<Project/>");
        directory.Write("Shared/Templates/Installation/modules/Directory.Build.props", "isolation");
        return new StellarInstallation(directory.Path);
    }

    public static StellarInstallation Installed(TempDirectory directory, EngineManifest? manifest = null)
    {
        var installation = Create(directory);
        EngineManifestStore.Save(installation.EngineManifest,
            manifest ?? new EngineManifest { OrchesterVersion = "0.7.0" });
        return installation;
    }

    public static string Module(TempDirectory directory, string folder, string id, string json = "",
        bool withProject = true)
    {
        var body = json.Length == 0 ? string.Empty : ", " + json;
        directory.Write($"{folder}/{id}.stellar.module", $$"""{ "Id": "{{id}}"{{body}} }""");

        if (withProject)
            directory.Write($"{folder}/src/Managed/{id}/{id}.csproj", "<Project/>");

        return Path.Combine(directory.Path, folder);
    }
}

public class ModuleDiscoveryTests
{
    [Fact]
    public void Discover_FindsModulesUpToTheDepth_AndNotInsideModules()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);

        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A");
        FakeCore.Module(directory, "modules/Group/Stellar.B", "Stellar.B");
        FakeCore.Module(directory, "modules/Stellar.A/inner/Stellar.C", "Stellar.C");
        FakeCore.Module(directory, "modules/One/Two/Stellar.D", "Stellar.D");
        FakeCore.Module(directory, "modules/.hidden/Stellar.E", "Stellar.E");

        var found = ModuleDiscovery.Discover(installation, new ModuleDiscoverySettings()).Select(Path.GetFileName);

        found.Should().BeEquivalentTo(["Stellar.A.stellar.module", "Stellar.B.stellar.module"]);
    }

    [Fact]
    public void Discover_CanBeSwitchedOff()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A");

        ModuleDiscovery.Discover(installation, new ModuleDiscoverySettings { Enabled = false }).Should().BeEmpty();
    }

    [Fact]
    public void Descriptor_MustBeNamedAfterItsModule()
    {
        using var directory = new TempDirectory();
        var path = directory.Write("Stellar.A.stellar.module", """{ "Id": "Stellar.B" }""");

        var action = () => ModuleDescriptorFile.Read(path);

        action.Should().Throw<ToolException>().WithMessage("*Stellar.B.stellar.module*");
    }

    [Fact]
    public void Conventions_FindTheProjectsOfAModule()
    {
        using var directory = new TempDirectory();
        directory.Write("src/Managed/Stellar.A/Stellar.A.csproj", "<Project/>");
        directory.Write("src/Native/Stellar.Native.A/Stellar.Native.A.vcxproj", "<Project/>");
        directory.Write("src/NativeInterop/Stellar.A.Wrapper/Stellar.A.Wrapper.vcxproj", "<Project/>");
        directory.Write("src/Native/Stellar.Native.A/obj/Ignored.vcxproj", "<Project/>");
        directory.Write("tests/Stellar.A.Tests/Stellar.A.Tests.csproj", "<Project/>");

        ModuleConventions.FindManagedProject(directory.Path, "Stellar.A").Should()
            .Be(Path.Combine("src", "Managed", "Stellar.A", "Stellar.A.csproj"));
        ModuleConventions.FindNativeProjects(directory.Path).Select(Path.GetFileName)
            .Should().Equal("Stellar.Native.A.vcxproj", "Stellar.A.Wrapper.vcxproj");
        ModuleConventions.FindTestProjects(directory.Path).Should().ContainSingle();
    }
}

public class ModuleResolverTests
{
    [Fact]
    public void Resolve_FillsTheDefaults()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A");

        var module = ModuleResolver.Resolve(installation, new EngineManifest()).Modules.Should().ContainSingle()
            .Subject;

        module.Source.Should().Be(ModuleSource.Discovered);
        module.Descriptor.Version.Should().Be(StellarVersions.Engine);
        module.Descriptor.Level.Should().Be(ModuleLevel.HighLevel);
        module.ProjectPath.Should().EndWith(Path.Combine("src", "Managed", "Stellar.A", "Stellar.A.csproj"));
    }

    [Fact]
    public void Resolve_TheManifestWinsOverTheDescriptor()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A", """ "Version": "1.0.0", "Level": "CoreModule" """);

        var manifest = new EngineManifest
        {
            Modules = [new ModuleOverride { Id = "Stellar.A", Version = "2.0.0", Link = ModuleLink.Package }]
        };

        var module = ModuleResolver.Resolve(installation, manifest).Modules.Single();

        module.Descriptor.Version.Should().Be("2.0.0");
        module.Descriptor.Level.Should().Be(ModuleLevel.CoreModule);
        module.Link.Should().Be(ModuleLink.Package);
    }

    [Fact]
    public void Resolve_AFolderOfTheManifestWinsOverADiscoveredOne()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A");
        var external = FakeCore.Module(directory, "work/Stellar.A", "Stellar.A");

        var manifest = new EngineManifest { Modules = [new ModuleOverride { Id = "Stellar.A", Path = external }] };

        var module = ModuleResolver.Resolve(installation, manifest).Modules.Single();

        module.Source.Should().Be(ModuleSource.External);
        module.Directory.Should().Be(Path.GetFullPath(external));
    }

    [Fact]
    public void Resolve_DisabledModules_AndWhoDependsOnThem()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A", """ "Level": "CoreModule" """);
        FakeCore.Module(directory, "modules/Stellar.B", "Stellar.B", """ "Dependencies": ["Stellar.A"] """);

        var manifest = new EngineManifest { Modules = [new ModuleOverride { Id = "Stellar.A", Enabled = false }] };

        var resolution = ModuleResolver.Resolve(installation, manifest);

        resolution.Disabled.Should().Equal("Stellar.A");
        resolution.Errors.Should().ContainSingle().Which.Should().Contain("turned off");
    }

    [Fact]
    public void Resolve_APackageOnlyModuleNeedsAVersion()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);

        var withoutVersion = ModuleResolver.Resolve(installation,
            new EngineManifest { Modules = [new ModuleOverride { Id = "Stellar.P" }] });
        var withVersion = ModuleResolver.Resolve(installation,
            new EngineManifest { Modules = [new ModuleOverride { Id = "Stellar.P", Version = "1.0.0" }] });

        withoutVersion.Errors.Should().ContainSingle().Which.Should().Contain("Version");
        withVersion.Modules.Single().Should().Match<ResolvedModule>(module =>
            module.Source == ModuleSource.Package && module.Link == ModuleLink.Package);
    }

    [Fact]
    public void Resolve_RejectsDuplicatesAndTheCore()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);
        FakeCore.Module(directory, "modules/One/Stellar.A", "Stellar.A");
        FakeCore.Module(directory, "modules/Two/Stellar.A", "Stellar.A");
        FakeCore.Module(directory, "modules/Stellar.Core", "Stellar.Core");

        var errors = ModuleResolver.Resolve(installation, new EngineManifest()).Errors;

        errors.Should().Contain(error => error.Contains("found twice")).And
            .Contain(error => error.Contains("part of the core"));
    }

    [Fact]
    public void Resolve_ImplicitDependenciesAreDropped()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Create(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A", """ "Dependencies": ["Stellar.Core"] """);

        var resolution = ModuleResolver.Resolve(installation, new EngineManifest());

        resolution.Modules.Single().Descriptor.Dependencies.Should().BeEmpty();
        resolution.Warnings.Should().ContainSingle().Which.Should().Contain("implicit");
    }
}

public class ModuleSynchronizerTests
{
    [Fact]
    public void Synchronize_WritesTheCatalog_ThatTellsAModuleWhoItIs()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Installed(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A", """ "Level": "CoreModule", "Version": "1.2.3" """);
        FakeCore.Module(directory, "modules/Stellar.B", "Stellar.B",
            """ "Dependencies": ["Stellar.A"], "Description": "B & co" """);

        var set = ModuleSynchronizer.Synchronize(installation, NullToolLogger.Instance);
        var catalog = File.ReadAllText(installation.ModuleCatalog);

        set.LoadOrder.Select(module => module.Id).Should().Equal("Stellar.A", "Stellar.B");
        catalog.Should()
            .Contain(
                """<StellarModuleInfo Include="Stellar.A" Version="[1.2.3]" Level="CoreModule" Link="Default" Source="Discovered" """
                    .TrimEnd());
        catalog.Should().Contain("""Condition="'$(StellarModuleId)' == 'Stellar.B'" """.TrimEnd());
        catalog.Should()
            .Contain(
                """<StellarModuleDependencies Condition="'$(StellarModuleDependencies)' == ''">Stellar.A</StellarModuleDependencies>""");
        catalog.Should().Contain("B &amp; co");
        File.ReadAllText(installation.ModuleLock).Should().Contain("\"Stellar.B\"");
    }

    [Fact]
    public void Synchronize_RefusesALevelViolation_AndWritesNothing()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Installed(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A",
            """ "Level": "CoreModule", "Dependencies": ["Stellar.B"] """);
        FakeCore.Module(directory, "modules/Stellar.B", "Stellar.B");

        var action = () => ModuleSynchronizer.Synchronize(installation, NullToolLogger.Instance);

        action.Should().Throw<ToolException>().WithMessage("*must not depend on*");
        File.Exists(installation.ModuleCatalog).Should().BeFalse();
    }
}

public class ModuleScaffolderTests
{
    [Fact]
    public void Create_RendersTheTemplateIntoModules()
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Installed(directory);
        directory.Write("Shared/Templates/Modules/Managed/__ModuleId__.stellar.module",
            """{ "Id": "{{ModuleId}}", "Level": "{{ModuleLevel}}" }""");
        directory.Write("Shared/Templates/Modules/Managed/src/Native/__ModuleNativeId__/__ModuleNativeId__.vcxproj",
            "{{ModuleNativeGuid}}");

        var created = new ModuleScaffolder(installation, NullToolLogger.Instance)
            .Create(new ModuleRequest("Stellar.Sample", ModuleLevel.CoreModule, null, "Managed"));

        created.Should().Be(Path.Combine(installation.Modules, "Stellar.Sample"));
        ModuleDescriptorFile.Read(Path.Combine(created, "Stellar.Sample.stellar.module")).Level.Should()
            .Be(ModuleLevel.CoreModule);
        File.ReadAllText(Path.Combine(created, "src", "Native", "Stellar.Native.Sample",
            "Stellar.Native.Sample.vcxproj")).Should().StartWith("{");
    }

    [Theory]
    [InlineData("sample")]
    [InlineData("Stellar")]
    [InlineData("Stellar.Core")]
    [InlineData("Stellar..A")]
    public void Create_RejectsBadIdentifiers(string id)
    {
        using var directory = new TempDirectory();
        var installation = FakeCore.Installed(directory);
        directory.Write("Shared/Templates/Modules/Managed/x.txt", "x");

        var action = () => new ModuleScaffolder(installation, NullToolLogger.Instance)
            .Create(new ModuleRequest(id, ModuleLevel.HighLevel, null, "Managed"));

        action.Should().Throw<ToolException>();
    }
}

public class ModuleBuildPlannerTests
{
    private static (StellarInstallation Installation, ModuleSet Set) Installation(TempDirectory directory)
    {
        var installation = FakeCore.Installed(directory);
        FakeCore.Module(directory, "modules/Stellar.A", "Stellar.A", """ "Level": "CoreModule" """);
        directory.Write("modules/Stellar.A/src/Native/Stellar.Native.A/Stellar.Native.A.vcxproj", "<Project/>");
        FakeCore.Module(directory, "modules/Stellar.B", "Stellar.B", """ "Dependencies": ["Stellar.A"] """);
        FakeCore.Module(directory, "modules/Stellar.C", "Stellar.C");

        return (installation, ModuleSynchronizer.Synchronize(installation, NullToolLogger.Instance));
    }

    [Fact]
    public void Plan_BuildsTheDependenciesFirst_NativeBeforeManaged()
    {
        using var directory = new TempDirectory();
        var (installation, set) = Installation(directory);

        var steps = ModuleBuildPlanner.Plan(installation, set, ["Stellar.B"], new ModuleBuildOptions(Pack: true),
            "msbuild.exe");

        steps.Select(step => $"{step.Module}: {step.Description}").Should().Equal(
            "Stellar.A: native Stellar.Native.A",
            "Stellar.A: build Stellar.A",
            "Stellar.A: pack Stellar.A",
            "Stellar.B: build Stellar.B",
            "Stellar.B: pack Stellar.B");
        steps.Should().OnlyContain(step =>
            step.Arguments.Any(argument => argument.StartsWith("-p:StellarRoot=") && argument.EndsWith("/")));
    }

    [Fact]
    public void Plan_NeedsMSBuildForNativeProjects_UnlessTheyAreSkipped()
    {
        using var directory = new TempDirectory();
        var (installation, set) = Installation(directory);

        var withNative = () => ModuleBuildPlanner.Plan(installation, set, [], new ModuleBuildOptions(), null);
        var withoutNative = ModuleBuildPlanner.Plan(installation, set, [], new ModuleBuildOptions(Native: false), null);

        withNative.Should().Throw<ToolException>().WithMessage("*--no-native*");
        withoutNative.Should().HaveCount(3);
    }

    [Fact]
    public void Builder_StopsAtTheFirstFailure()
    {
        var runner = new RecordingRunner(1);
        var steps = new[]
        {
            new BuildStep("A", "one", "dotnet", ["build"]),
            new BuildStep("A", "two", "dotnet", ["build"]),
            new BuildStep("B", "three", "dotnet", ["build"])
        };

        var action = () => new ModuleBuilder(runner, NullToolLogger.Instance).Run(steps, ".");

        action.Should().Throw<ToolException>().WithMessage("*two*");
        runner.Calls.Should().Be(2);
    }

    private sealed class RecordingRunner(int failAt) : IProcessRunner
    {
        public int Calls { get; private set; }

        public int Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
        {
            return Calls++ == failAt ? 1 : 0;
        }
    }
}

public class ManifestEditorTests
{
    [Fact]
    public void Entry_IsCreatedOnce_AndCompactDropsEmptyOnes()
    {
        var manifest = new EngineManifest();

        EngineManifestEditor.Entry(manifest, "Stellar.B").Enabled = false;
        EngineManifestEditor.Entry(manifest, "Stellar.A");
        EngineManifestEditor.Entry(manifest, "Stellar.B").Path = "elsewhere";

        manifest.Modules.Select(entry => entry.Id).Should().Equal("Stellar.A", "Stellar.B");

        EngineManifestEditor.Compact(manifest);
        manifest.Modules.Should().ContainSingle().Which.Path.Should().Be("elsewhere");

        EngineManifestEditor.Reset(manifest, "Stellar.B").Should().BeTrue();
        manifest.Modules.Should().BeEmpty();
    }
}
