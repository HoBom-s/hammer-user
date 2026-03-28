using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Infrastructure.OAuth;

/// <summary>
///     Client for a specific OAuth provider.
/// </summary>
internal interface IOAuthProviderClient
{
    /// <summary>
    ///     Gets the OAuth provider this client handles.
    /// </summary>
    public OAuthProvider Provider { get; }

    /// <summary>
    ///     Validates the token and retrieves user information.
    /// </summary>
    /// <param name="token">The provider-issued token.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user information from the provider.</returns>
    public Task<OAuthUserInfo> GetUserInfoAsync(string token, CancellationToken ct);
}
