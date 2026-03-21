namespace Hammer.User.Domain.Common;

/// <summary>
/// Abstract base class for all domain entities.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class.
    /// </summary>
    protected Entity()
    {
        Id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>
    /// Gets the last updated timestamp in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; protected set; }
}
