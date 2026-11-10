using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Stellar.Tools.Diagnostics;

namespace Stellar.Sdk.Tasks;

/// <summary>
/// Base of the SDK tasks. A task only translates between MSBuild and Stellar.Tools: the logic lives in Tools,
/// where the CLI and the Editor can reach it too.
/// </summary>
public abstract class StellarTask
    : Task, IToolLogger
{
    /// <inheritdoc/>
    public sealed override bool Execute()
    {
        try
        {
            Run();
        }
        catch (ToolException exception)
        {
            Log.LogError(exception.Message);
        }
        catch (Exception exception)
        {
            Log.LogErrorFromException(exception, true);
        }

        return !Log.HasLoggedErrors;
    }

    /// <summary>
    /// Does the work of the task.
    /// </summary>
    protected abstract void Run();

    void IToolLogger.Log(ToolMessageLevel level, string message)
    {
        switch (level)
        {
            case ToolMessageLevel.Verbose:
                Log.LogMessage(MessageImportance.Low, message);
                break;
            case ToolMessageLevel.Info:
                Log.LogMessage(MessageImportance.Normal, message);
                break;
            case ToolMessageLevel.Warning:
                Log.LogWarning(message);
                break;
            case ToolMessageLevel.Error:
                Log.LogError(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
