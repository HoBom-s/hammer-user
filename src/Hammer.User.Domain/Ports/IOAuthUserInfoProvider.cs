using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Ports;

/// <summary>
///     Resolves user information from an OAuth provider token.
/// </summary>
public interface IOAuthUserInfoProvider
{
    /// <summary>
    ///     Validates the token and retrieves user information from the specified provider.
    /// </summary>
    /// <param name="provider">The OAuth provider.</param>
    /// <param name="token">The provider-issued token (ID token or access token).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The user information from the provider.</returns>
    public Task<OAuthUserInfo> GetUserInfoAsync(OAuthProvider provider, string token, CancellationToken ct);
}
