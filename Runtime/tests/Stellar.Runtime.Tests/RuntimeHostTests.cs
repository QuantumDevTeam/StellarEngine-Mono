using Stellar.Kernel.Configuration;
using Stellar.Kernel.Configuration.Modules;
using Stellar.Kernel.Modules;
using Stellar.Runtime.Configuration;
using Stellar.Runtime.Diagnostics;
using Stellar.Runtime.Hosting;

namespace Stellar.Runtime.Tests;

/// <summary>
/// The whole life of the engine, with the fixtures: a host module that owns the services, and a game.
/// </summary>
[Collection(ProcessDataCollection.Name)]
public sealed class RuntimeHostTests : IDisposable
{
    private const string Application = "Stellar.Runtime.Tests.Application";
    private const string BehaviorKey = "Stellar.Runtime.Tests.Behavior";
    private const string EventsKey = "Stellar.Runtime.Tests.Events";

    private readonly List<string> _events = [];
    private readonly StringWriter _output = new();

    public RuntimeHostTests()
    {
        AppContext.SetData(EventsKey, _events);
        AppContext.SetData(BehaviorKey, null);
    }

    private static string Fixtures => Path.Combine(AppContext.BaseDirectory, "fixtures");

    public void Dispose()
    {
        AppContext.SetData(EventsKey, null);
        AppContext.SetData(BehaviorKey, null);
    }

    private int Run(string? behavior = null, string[]? arguments = null, RuntimeConfigurationLocator? locator = null)
    {
        AppContext.SetData(BehaviorKey, behavior);

        using var host = new RuntimeHost(new RuntimeLaunch(Application, arguments, Fixtures),
            log: new RuntimeLog(fallback: _output));

        if (locator is not null) host.Context.ConfigurationLocator = locator;

        return host.Run();
    }

    [Fact]
    public void Run_StartsTheModules_ThenTheGame_RunsFrames_ThenStopsInReverse()
    {
        Run().Should().Be(RuntimeExitCodes.Success, _output.ToString());

        _events.Should().StartWith(
        [
            "host:run control=True",
            "app:run context=Stellar.Module:Stellar.Runtime.Tests.Application host=Default loop=True main=True engine=Stellar.Runtime.Tests.Application clock=Engine",
            "app:update 0",
            "app:render 0",
            "app:update 1",
            "app:render 1",
            "app:update 2"
        ]);

        _events.Should().EndWith(["app:stop Regular", "app:dispose", "host:stop Regular", "host:dispose"]);
    }

    [Fact]
    public void Run_AFailingFrame_IsReported_AndTheLoopGoesOn()
    {
        Run("throw-update").Should().Be(RuntimeExitCodes.Success, _output.ToString());

        _events.Should().Contain("report:Update:a bad frame");
        _events.Should().Contain("app:update 2");
    }

    [Fact]
    public void Run_AGameThatDoesNotStart_ExitsWithItsCode_AndTheEngineStops()
    {
        Run("fail-run").Should().Be(3);

        _events.Should().NotContain(e => e.StartsWith("app:update"));
        _events.Should().EndWith(["app:stop CriticalError", "app:dispose", "host:stop CriticalError", "host:dispose"]);
    }

    [Fact]
    public void Run_AGameThatThrows_IsReportedAsCritical()
    {
        Run("throw-run").Should().Be(RuntimeExitCodes.Failure);

        _events.Should().Contain(e => e.StartsWith("report:Engine:the game cannot start"));
        _events.Should().EndWith("host:dispose");
    }

    [Fact]
    public void Run_MaxFrames_FromTheCommandLine_StopsTheLoop()
    {
        Run(arguments: ["--stellar:MaxFrames=1"]).Should().Be(RuntimeExitCodes.Success);

        _events.Should().Contain("app:update 0").And.NotContain("app:update 1");
    }

    [Fact]
    public void Run_WithoutMainLoop_StopsOnceTheGameStarted()
    {
        Run(arguments: ["--stellar:MainLoop=false"]).Should().Be(RuntimeExitCodes.Success);

        _events.Should().Contain(e => e.StartsWith("app:run")).And.NotContain(e => e.StartsWith("app:update"));
    }

    [Fact]
    public void Run_AMissingModule_IsAStartupFailure_AndNothingRuns()
    {
        var locator = new RuntimeConfigurationLocator([
            new FixedSource(new RuntimeConfiguration
            {
                ProjectName = Application,
                EntryPoint = "Stellar.Runtime.Tests.Application.Game",
                Components =
                [
                    new ModulesComponent
                    {
                        Modules =
                        [
                            new RuntimeModuleData { Id = "Stellar.Runtime.Tests.Host", Level = ModuleLevel.Core },
                            new RuntimeModuleData { Id = "Stellar.Missing", Level = ModuleLevel.HighLevel }
                        ]
                    }
                ]
            })
        ]);

        Run(locator: locator).Should().Be(RuntimeExitCodes.StartupFailure);

        _events.Should().BeEmpty();
        _output.ToString().Should().Contain("Stellar.Missing");
    }

    [Fact]
    public void Run_AMissingApplication_IsAStartupFailure()
    {
        using var host = new RuntimeHost(new RuntimeLaunch("Nothing", baseDirectory: Fixtures),
            log: new RuntimeLog(fallback: _output));

        host.Run().Should().Be(RuntimeExitCodes.StartupFailure);
        _output.ToString().Should().Contain("Nothing.dll");
    }

    private sealed class FixedSource(RuntimeConfiguration configuration)
        : IRuntimeConfigurationSource
    {
        public RuntimeConfigurationSource Kind => RuntimeConfigurationSource.External;

        public RuntimeConfiguration Read(string assemblyPath, string name)
        {
            return configuration;
        }
    }
}

/// <summary>
/// The tests that set data of the process run one at a time.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessDataCollection
{
    public const string Name = "Process data";
}
