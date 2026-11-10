using Stellar.Kernel.Configuration;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Modules;
using Stellar.Runtime.Configuration;
using Stellar.Runtime.Modules;

namespace Stellar.Runtime.Tests;

[Collection(ProcessDataCollection.Name)]
public class ConfigurationTests
{
    private static string Fixture(string name)
    {
        return Path.Combine(AppContext.BaseDirectory, "fixtures", name + ".dll");
    }

    [Fact]
    public void AssemblyFile_ReadsTheAttribute_WithoutLoadingTheAssembly()
    {
        var attribute = AssemblyFile.ReadConfigurationAttribute(Fixture("Stellar.Runtime.Tests.Application"));

        attribute.Should().NotBeNull();
        attribute!.Source.Should().Be(RuntimeConfigurationSource.External);
        attribute.Name.Should().Be("Stellar.Runtime.Tests.Application.stellar.runtime.config.json");

        AppDomain.CurrentDomain.GetAssemblies().Should()
            .NotContain(a => a.GetName().Name == "Stellar.Runtime.Tests.Application");
    }

    [Fact]
    public void AssemblyFile_NoAttribute_IsNull()
    {
        AssemblyFile.ReadConfigurationAttribute(typeof(ConfigurationTests).Assembly.Location).Should().BeNull();
    }

    [Fact]
    public void Locator_ReadsAFileNextToTheAssembly()
    {
        var configuration = new RuntimeConfigurationLocator().Read(Fixture("Stellar.Runtime.Tests.Application"));

        configuration.EntryPoint.Should().Be("Stellar.Runtime.Tests.Application.Game");
        configuration.Components.OfType<ModulesComponent>().Single().Modules
            .Select(module => module.Level).Should().Equal(ModuleLevel.Kernel, ModuleLevel.Core);
    }

    [Fact]
    public void Locator_ReadsAnEmbeddedResource()
    {
        var configuration = new RuntimeConfigurationLocator().Read(Fixture("Stellar.Runtime.Tests.Host"));

        configuration.ProjectName.Should().Be("Host");
        configuration.Settings["MaxFrames"].Should().Be("7");
    }

    [Fact]
    public void Locator_MissingFile_SaysWhere()
    {
        using var directory = new TempFolder();
        var copy = Path.Combine(directory.Path, "Stellar.Runtime.Tests.Application.dll");
        File.Copy(Fixture("Stellar.Runtime.Tests.Application"), copy);

        var action = () => new RuntimeConfigurationLocator().Read(copy);

        action.Should().Throw<RuntimeConfigurationException>().WithMessage("*missing*stellar.runtime.config.json*");
    }

    [Fact]
    public void Settings_ProjectThenCommandLine()
    {
        var configuration = new RuntimeConfiguration
        {
            Settings = { ["TargetFrameRate"] = "60", ["MainLoop"] = "true", ["Custom"] = "a" }
        };

        var settings = RuntimeSettings.Create(configuration,
        [
            "--stellar:TargetFrameRate=30", "--stellar:Verbose", "--other",
            "--stellar:ModuleInitializationParallelism=0"
        ]);

        settings.TargetFrameRate.Should().Be(30);
        settings.Verbose.Should().BeTrue();
        settings.MainLoop.Should().BeTrue();
        settings.CollectibleModules.Should().BeTrue();
        settings.ModuleInitializationParallelism.Should().Be(1);
        settings.MaxDeltaTime.Should().Be(0.25);
        settings.Get("custom").Should().Be("a");
    }

    [Fact]
    public void Plan_RejectsDuplicates_AndWrongOrder()
    {
        var core = new RuntimeModuleData { Id = "Core", Level = ModuleLevel.Core };
        var graphic = new RuntimeModuleData { Id = "Graphic", Level = ModuleLevel.HighLevel, Dependencies = ["Core"] };

        ModulePlan.Create([core, graphic]).ByLevel().Select(group => group.Single().Id).Should()
            .Equal("Core", "Graphic");

        ((Action)(() => ModulePlan.Create([graphic, core]))).Should().Throw<ModuleLoadException>()
            .WithMessage("*before Core*");
        ((Action)(() => ModulePlan.Create([core, core]))).Should().Throw<ModuleLoadException>().WithMessage("*twice*");
    }

    [Fact]
    public void Plan_WithoutModules_AsksForARebuild()
    {
        var action = () => ModulePlan.From(new RuntimeConfiguration { ProjectName = "Old" });

        action.Should().Throw<ModuleLoadException>().WithMessage("*rebuild*");
    }

    private sealed class TempFolder : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("stellar-runtime-").FullName;

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
