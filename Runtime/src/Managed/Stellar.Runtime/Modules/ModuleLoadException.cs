namespace Stellar.Runtime.Modules;

/// <summary>
/// A module cannot be loaded or started.
/// </summary>
public sealed class ModuleLoadException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleLoadException"/> class.
    /// </summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The cause.</param>
    public ModuleLoadException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
