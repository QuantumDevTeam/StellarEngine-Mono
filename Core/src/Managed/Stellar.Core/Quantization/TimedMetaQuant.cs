using Stellar.Kernel;

namespace Stellar.Core.Quantization;

/// <summary>
/// A meta quant with a limited lifetime.
/// </summary>
public class TimedMetaQuant
    : MetaQuant
{
    /// <summary>
    /// Creates a timed meta quant.
    /// </summary>
    /// <param name="lifetime">The lifetime. When <c>null</c>, the meta quant never expires.</param>
    /// <param name="identifier">Unique ID. A new one is created when omitted.</param>
    public TimedMetaQuant(TimeSpan? lifetime = null, IIdentifier? identifier = null)
        : base(identifier ?? Identifier.CreateAndRegister())
    {
        CreatedAt = StartAt = DateTime.UtcNow;
        Lifetime = lifetime;
    }

    /// <summary>
    /// Gets the moment this meta quant was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the moment the lifetime is counted from (UTC).
    /// </summary>
    public DateTime StartAt { get; private set; }

    /// <summary>
    /// Gets the lifetime, or <c>null</c> if this meta quant never expires.
    /// </summary>
    public TimeSpan? Lifetime { get; private set; }

    /// <summary>
    /// Gets the time passed since creation.
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Gets the moment of expiration (UTC), or <c>null</c> if this meta quant never expires.
    /// </summary>
    public DateTime? ExpiresAt => StartAt + Lifetime;

    /// <summary>
    /// Gets the time left, or <c>null</c> if this meta quant never expires.
    /// </summary>
    public TimeSpan? ExpiresIn => ExpiresAt - DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the lifetime has run out.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Sets a new lifetime.
    /// </summary>
    /// <param name="lifetime">The lifetime.</param>
    /// <param name="startAt">The moment to count from. Now when omitted.</param>
    public void SetLifetime(TimeSpan lifetime, DateTime? startAt = null)
    {
        StartAt = startAt ?? DateTime.UtcNow;
        Lifetime = lifetime;
    }

    /// <summary>
    /// Restarts the lifetime from now.
    /// </summary>
    public void ResetLifetime()
    {
        StartAt = DateTime.UtcNow;
    }
}
