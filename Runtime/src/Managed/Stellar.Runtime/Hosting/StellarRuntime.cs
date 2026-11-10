namespace Stellar.Runtime.Hosting;

/// <summary>
/// The door into the engine: what the launcher of an application calls.
/// </summary>
/// <remarks>
/// <para>Stellar.Sdk generates the launcher of every executable module: an executable and a small assembly whose
/// <c>Main</c> calls <see cref="Run(string, string[])"/> with the name of the application. The application itself is
/// a module like any other; the runtime reads its configuration, loads the modules it was built with, starts the
/// engine, then the application, then runs the main loop.</para>
/// <para>The calling thread becomes the main thread of the engine.</para>
/// </remarks>
public static class StellarRuntime
{
    /// <summary>
    /// Runs an application until the engine stops.
    /// </summary>
    /// <param name="application">The simple name of the assembly of the application, next to the launcher.</param>
    /// <param name="arguments">The command line.</param>
    /// <returns>The exit code (<see cref="RuntimeExitCodes"/>).</returns>
    public static int Run(string application, string[] arguments)
    {
        return Run(new RuntimeLaunch(application, arguments));
    }

    /// <summary>
    /// Runs an application until the engine stops.
    /// </summary>
    /// <param name="launch">What to run.</param>
    /// <returns>The exit code (<see cref="RuntimeExitCodes"/>).</returns>
    public static int Run(RuntimeLaunch launch)
    {
        using var host = new RuntimeHost(launch);
        return host.Run();
    }
}
