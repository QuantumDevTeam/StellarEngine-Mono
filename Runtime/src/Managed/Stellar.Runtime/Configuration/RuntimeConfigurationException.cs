namespace Stellar.Runtime.Configuration;

/// <summary>
/// The runtime configuration of an assembly is missing or cannot be read.
/// </summary>
public sealed class RuntimeConfigurationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeConfigurationException"/> class.
    /// </summary>
    /// <param name="message">What is wrong.</param>
    /// <param name="innerException">The cause.</param>
    public RuntimeConfigurationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
