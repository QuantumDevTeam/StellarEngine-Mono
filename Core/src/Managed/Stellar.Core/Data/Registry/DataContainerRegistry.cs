using Stellar.Kernel;
using Stellar.Kernel.Data.Collections;

namespace Stellar.Core.Data.Registry;

/// <summary>
/// Global registry of data containers.
/// </summary>
public sealed class DataContainerRegistry
    : ConcurrentRegistry<IDataContainer>
{
    /// <summary>
    /// Creates an empty registry. Each engine has one (<see cref="Global.CoreEngine.Registry"/>).
    /// </summary>
    public DataContainerRegistry()
    {
    }

    /// <inheritdoc/>
    protected override IIdentifier IdentifierOf(IDataContainer obj)
    {
        return obj.UID;
    }
}
