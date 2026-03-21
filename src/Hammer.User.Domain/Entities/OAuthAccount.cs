using Hammer.User.Domain.Common;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Entities;

/// <summary>
/// Represents an OAuth provider account linked to a user.
/// </summary>
public sealed class OAuthAccount : Entity
{
    private OAuthAccount()
    {
    }

    /// <summary>
    /// Gets the owning user's identifier.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// Gets the OAuth provider.
    /// </summary>
    public OAuthProvider Provider { get; private init; }

    /// <summary>
    /// Gets the provider's subject identifier (sub claim).
    /// </summary>
    public string ProviderSubjectId { get; private init; } = string.Empty;

    /// <summary>
    /// Creates a new <see cref="OAuthAccount"/>.
    /// </summary>
    /// <param name="userId">The owning user's identifier.</param>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="providerSubjectId">The provider's subject identifier.</param>
    /// <returns>A new <see cref="OAuthAccount"/> instance.</returns>
    public static OAuthAccount Create(Guid userId, OAuthProvider provider, string providerSubjectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubjectId);

        return new OAuthAccount
        {
            UserId = userId,
            Provider = provider,
            ProviderSubjectId = providerSubjectId,
        };
    }
}
