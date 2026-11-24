namespace Stellar.Kernel.LoggingSystem;

/// <summary>
/// {0} - date yyyy-MMM-dd <br/>
/// {1} - time HH:mm:ss.fff <br/>
/// {2} - engine clock name <br/>
/// {3} - engine clock time HH:mm:ss.fff <br/>
/// {4} - LogLevel <br/>
/// {5} - Engine instance name <br/>
/// {6} - Logger instance name <br/>
/// {7} - Thread ID <br/>
/// {8} - C# Type name <br/>
/// {9} - C# Method name<br/>
/// {10} - message <br/>
/// </summary>
/// <param name="Format">Format string</param>
/// <param name="ColorizedFormat">Colorized Format string (ASCII)</param>
public record struct LogFormat(string Format, string? ColorizedFormat = null);
