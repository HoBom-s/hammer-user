namespace Hammer.User.Application.Common;

/// <summary>
///     JWT configuration settings.
/// </summary>
public sealed class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public int AccessTokenExpiryMinutes { get; init; }

    public int RefreshTokenExpiryDays { get; init; }
}
