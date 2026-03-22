using Hammer.User.Domain.Common;

namespace Hammer.User.Domain.Entities;

/// <summary>
/// Represents a JWT refresh token issued to a user.
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    /// <summary>
    /// Gets the owning user's identifier.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// Gets the token value.
    /// </summary>
    public string Token { get; private init; } = string.Empty;

    /// <summary>
    /// Gets the expiration timestamp in UTC.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private init; }

    /// <summary>
    /// Gets the revocation timestamp in UTC, or <c>null</c> if not revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this token has been revoked.
    /// </summary>
    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// Gets a value indicating whether this token has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Gets a value indicating whether this token is still usable.
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// Creates a new <see cref="RefreshToken"/>.
    /// </summary>
    /// <param name="userId">The owning user's identifier.</param>
    /// <param name="token">The token value.</param>
    /// <param name="expiresAt">The expiration timestamp.</param>
    /// <returns>A new <see cref="RefreshToken"/> instance.</returns>
    public static RefreshToken Create(Guid userId, string token, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
        };
    }

    /// <summary>
    /// Revokes this token. This operation is idempotent.
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
