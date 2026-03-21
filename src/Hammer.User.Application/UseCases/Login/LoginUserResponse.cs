namespace Hammer.User.Application.UseCases.Login;

/// <summary>
///     Response DTO for user login.
/// </summary>
public sealed record LoginUserResponse(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);
