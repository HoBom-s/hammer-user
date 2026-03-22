namespace Hammer.User.Domain.Ports;

/// <summary>
///     Abstraction for JWT access token and refresh token generation.
/// </summary>
public interface IJwtTokenGenerator
{
    public string GenerateAccessToken(Guid userId, string email, string nickname);

    public string GenerateRefreshToken();

    /// <summary>
    ///     Validates the token's signature, issuer, audience, and lifetime.
    /// </summary>
    /// <param name="token">The JWT access token to validate.</param>
    /// <returns>Parsed claims on success, or <c>null</c> if invalid.</returns>
    public AccessTokenClaims? ValidateAccessToken(string token);
}
