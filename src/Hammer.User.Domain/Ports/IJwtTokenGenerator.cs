namespace Hammer.User.Domain.Ports;

/// <summary>
/// Abstraction for JWT access token and refresh token generation.
/// </summary>
public interface IJwtTokenGenerator
{
    public string GenerateAccessToken(Guid userId, string email, string nickname);

    public string GenerateRefreshToken();
}
