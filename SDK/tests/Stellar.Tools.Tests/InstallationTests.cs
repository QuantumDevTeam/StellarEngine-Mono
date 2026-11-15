using System.Diagnostics;
using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Installation;

namespace Stellar.Tools.Tests;

public class InstallationRegistryTests
{
    [Fact]
    public void MissingRegistry_IsEmpty()
    {
        using var directory = new TempDirectory();
        var registry = new InstallationRegistry(Path.Combine(directory.Path, "home"));

        registry.List().Should().BeEmpty();
        registry.Find("0.8.0").Should().BeNull();
        Directory.Exists(registry.Directory).Should().BeFalse();
    }

    [Fact]
    public void Register_KeepsOneInstallationPerOrchestra_AndWritesTheMSBuildView()
    {
        using var directory = new TempDirectory();
        var registry = new InstallationRegistry(Path.Combine(directory.Path, "home"));
        var first = new StellarInstallation(Path.Combine(directory.Path, "first"));
        var second = new StellarInstallation(Path.Combine(directory.Path, "second"));
        var other = new StellarInstallation(Path.Combine(directory.Path, "other"));

        registry.Register("0.8.0", first);
        registry.Register("0.8.0", second);
        registry.Register("0.9.0", other);

        registry.List().Select(entry => entry.OrchesterVersion).Should().Equal("0.8.0", "0.9.0");
        registry.Find("0.8.0")!.Root.Should().Be(second.Root);
        registry.Find("0.9.0")!.Root.Should().Be(other.Root);

        var props = File.ReadAllText(registry.MSBuildFile);
        props.Should().Contain("'$(StellarOrchesterVersion)' == '0.8.0'\">" + second.Root + Path.DirectorySeparatorChar);
        props.Should().Contain("'$(StellarOrchesterVersion)' == '0.9.0'\">" + other.Root + Path.DirectorySeparatorChar);
        props.Should().NotContain(first.Root + Path.DirectorySeparatorChar + "<");
    }

    [Fact]
    public void Unregister_RemovesEveryVersionOfTheRoot_AndTheLastOneLeavesNothing()
    {
        using var directory = new TempDirectory();
        var registry = new InstallationRegistry(Path.Combine(directory.Path, "home"));
        var installation = new StellarInstallation(Path.Combine(directory.Path, "engine"));
        var other = new StellarInstallation(Path.Combine(directory.Path, "other"));

        registry.Register("0.8.0", installation);
        registry.Register("0.9.0", installation);
        registry.Register("1.0.0", other);

        registry.Unregister(installation).Should().BeEquivalentTo("0.8.0", "0.9.0");
        registry.List().Should().ContainSingle().Which.OrchesterVersion.Should().Be("1.0.0");

        registry.Unregister(other).Should().Equal("1.0.0");
        Directory.Exists(registry.Directory).Should().BeFalse();
    }

    [Fact]
    public void TryLocate_FindsTheInstallationOfThisOrchestraInTheRegistry()
    {
        if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable(StellarInstallation.EnvironmentVariable)))
            return; // $STELLAR_ROOT wins over the registry, by design

        using var directory = new TempDirectory();
        var registry = new InstallationRegistry(Path.Combine(directory.Path, "home"));
        var installation = new StellarInstallation(Path.Combine(directory.Path, "engine"));
        registry.Register(StellarVersions.Orchester, installation);

        StellarInstallation.TryLocate(directory.Path, registry)!.Root.Should().Be(installation.Root);
    }
}

public class UninstallerTests
{
    private static (StellarInstallation Installation, InstallationRegistry Registry, string Cache) Installed(
        TempDirectory directory)
    {
        directory.Write("Shared/Templates/Installation/config/Stellar.Sdk.props", "settings {{OrchesterVersion}}");
        directory.Write("Shared/Templates/Installation/projects/Directory.Build.props", "isolation");
        var installation = FakeCore.Create(directory);
        var registry = new InstallationRegistry(Path.Combine(directory.Path, "home"));

        EngineInstaller.Install(installation, NullToolLogger.Instance, registry);

        directory.Write("packages/Stellar.Kernel.0.8.0.nupkg");
        directory.Write("packages/Stellar.Sdk.0.4.0-preview.1.nupkg");
        var cache = Path.Combine(directory.Path, "nuget");
        directory.Write("nuget/stellar.kernel/0.8.0/stellar.kernel.nuspec");
        directory.Write("nuget/stellar.kernel/0.7.0/stellar.kernel.nuspec");
        directory.Write("nuget/stellar.sdk/0.4.0-preview.1/stellar.sdk.nuspec");
        directory.Write("nuget/newtonsoft.json/13.0.3/newtonsoft.json.nuspec");

        return (installation, registry, cache);
    }

    [Fact]
    public void Uninstall_RefusesADirectoryThatIsNotTheCore()
    {
        using var directory = new TempDirectory();

        var action = () => EngineUninstaller.Uninstall(new StellarInstallation(directory.Path),
            NullToolLogger.Instance, new UninstallOptions());

        action.Should().Throw<ToolException>().WithMessage("*not a checkout of StellarEngine-Core*");
    }

    [Fact]
    public void Uninstall_LeavesAPlainCheckout_AndNeverTouchesTheContentOfTheUser()
    {
        using var directory = new TempDirectory();
        var (installation, registry, cache) = Installed(directory);
        directory.Write("modules/Stellar.FileSystem/Stellar.FileSystem.stellar.module", "{}");
        directory.Write("projects/MyGame/MyGame.csproj", "<Project/>");
        directory.Write("tools/StellarEditor/README.md");

        var report = EngineUninstaller.Uninstall(installation, NullToolLogger.Instance,
            new UninstallOptions { Registry = registry, PackageCache = cache });

        // The machine forgot it
        report.Unregistered.Should().Equal(StellarVersions.Orchester);
        registry.List().Should().BeEmpty();

        // Exactly the packages of this installation left the cache
        Directory.Exists(Path.Combine(cache, "stellar.kernel", "0.8.0")).Should().BeFalse();
        Directory.Exists(Path.Combine(cache, "stellar.sdk", "0.4.0-preview.1")).Should().BeFalse();
        Directory.Exists(Path.Combine(cache, "stellar.kernel", "0.7.0")).Should().BeTrue();
        Directory.Exists(Path.Combine(cache, "newtonsoft.json", "13.0.3")).Should().BeTrue();

        // What the installer made is gone
        foreach (var path in new[]
                 {
                     installation.Packages, installation.Manifests, installation.Config,
                     Path.Combine(installation.Modules, "Directory.Build.props"),
                     Path.Combine(installation.Projects, "Directory.Build.props"), installation.ResourceCompilers
                 })
            Path.Exists(path).Should().BeFalse(path);

        // The content of the user stays, and so do the folders that hold it
        File.Exists(Path.Combine(installation.Modules, "Stellar.FileSystem", "Stellar.FileSystem.stellar.module"))
            .Should().BeTrue();
        File.Exists(Path.Combine(installation.Projects, "MyGame", "MyGame.csproj")).Should().BeTrue();
        File.Exists(Path.Combine(installation.Tools, "StellarEditor", "README.md")).Should().BeTrue();
        installation.HasCoreSources.Should().BeTrue();
        installation.IsInstalled.Should().BeFalse();
    }

    [Fact]
    public void Uninstall_RemovesEmptyFoldersOfTheInstaller()
    {
        using var directory = new TempDirectory();
        var (installation, registry, cache) = Installed(directory);

        EngineUninstaller.Uninstall(installation, NullToolLogger.Instance,
            new UninstallOptions { Registry = registry, PackageCache = cache });

        foreach (var path in new[] { installation.Modules, installation.Projects, installation.Tools })
            Directory.Exists(path).Should().BeFalse(path);
    }

    [Fact]
    public void Uninstall_KeepsEditedFiles_UnlessAll()
    {
        using var directory = new TempDirectory();
        var (installation, registry, _) = Installed(directory);
        var settings = Path.Combine(installation.Config, "Stellar.Sdk.props");
        File.WriteAllText(settings, "my settings");

        var report = EngineUninstaller.Uninstall(installation, NullToolLogger.Instance,
            new UninstallOptions { Registry = registry });

        File.ReadAllText(settings).Should().Be("my settings");
        report.Kept.Should().Contain(entry => entry.StartsWith(settings));

        EngineUninstaller.Uninstall(installation, NullToolLogger.Instance,
            new UninstallOptions { Registry = registry, IncludeEdited = true });

        Directory.Exists(installation.Config).Should().BeFalse();
    }

    [Fact]
    public void Uninstall_DryRun_ChangesNothing()
    {
        using var directory = new TempDirectory();
        var (installation, registry, cache) = Installed(directory);

        var report = EngineUninstaller.Uninstall(installation, NullToolLogger.Instance,
            new UninstallOptions { Registry = registry, PackageCache = cache, DryRun = true });

        report.Unregistered.Should().Equal(StellarVersions.Orchester);
        report.Removed.Should().Contain(installation.Packages).And.Contain(installation.Modules);
        registry.Find(StellarVersions.Orchester).Should().NotBeNull();
        installation.IsInstalled.Should().BeTrue();
        Directory.Exists(installation.Packages).Should().BeTrue();
        Directory.Exists(Path.Combine(cache, "stellar.kernel", "0.8.0")).Should().BeTrue();
    }

    [Fact]
    public void Uninstall_RemovesALinkAsALink_NeverWhatItPointsAt()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var directory = new TempDirectory();
        using var outside = new TempDirectory();
        var precious = outside.Write("precious.txt", "keep me");
        var (installation, registry, _) = Installed(directory);

        var junction = Path.Combine(installation.Packages, "linked");
        using (var mklink = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junction}\" \"{outside.Path}\"")
                   { CreateNoWindow = true, UseShellExecute = false }))
            mklink!.WaitForExit();
        Directory.Exists(junction).Should().BeTrue("the junction must exist for this test to mean anything");

        EngineUninstaller.Uninstall(installation, NullToolLogger.Instance, new UninstallOptions { Registry = registry });

        Directory.Exists(installation.Packages).Should().BeFalse();
        File.ReadAllText(precious).Should().Be("keep me");
    }
}
