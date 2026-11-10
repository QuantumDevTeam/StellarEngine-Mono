namespace Stellar.Runtime.Hosting;

/// <summary>
/// What the runtime is asked to run.
/// </summary>
public sealed record RuntimeLaunch
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeLaunch"/> class.
    /// </summary>
    /// <param name="application">
    /// The application: the simple name of its assembly, looked up in <paramref name="baseDirectory"/>, or a path.
    /// </param>
    /// <param name="arguments">The command line.</param>
    /// <param name="baseDirectory">The folder of the application; the folder of the process by default.</param>
    public RuntimeLaunch(string application, IReadOnlyList<string>? arguments = null, string? baseDirectory = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(application);

        BaseDirectory = Path.GetFullPath(baseDirectory ?? AppContext.BaseDirectory);
        ApplicationPath = application.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(application, BaseDirectory)
            : Path.Combine(BaseDirectory, application + ".dll");
        Arguments = arguments ?? [];
    }

    /// <summary>
    /// Gets the folder of the application.
    /// </summary>
    public string BaseDirectory { get; }

    /// <summary>
    /// Gets the assembly of the application.
    /// </summary>
    public string ApplicationPath { get; }

    /// <summary>
    /// Gets the simple name of the assembly of the application.
    /// </summary>
    public string ApplicationName => Path.GetFileNameWithoutExtension(ApplicationPath);

    /// <summary>
    /// Gets the command line.
    /// </summary>
    public IReadOnlyList<string> Arguments { get; }
}
