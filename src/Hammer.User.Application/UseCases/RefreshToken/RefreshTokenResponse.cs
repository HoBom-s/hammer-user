namespace Hammer.User.Application.UseCases.RefreshToken;

/// <summary>
///     Response DTO for token refresh.
/// </summary>
public sealed record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
