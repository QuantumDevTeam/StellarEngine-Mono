#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Compiler support for nullable analysis, missing from netstandard2.0.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class MaybeNullWhenAttribute(bool returnValue)
    : Attribute
{
    public bool ReturnValue { get; } = returnValue;
}

/// <summary>
/// Compiler support for nullable analysis, missing from netstandard2.0.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class NotNullWhenAttribute(bool returnValue)
    : Attribute
{
    public bool ReturnValue { get; } = returnValue;
}
#endif
