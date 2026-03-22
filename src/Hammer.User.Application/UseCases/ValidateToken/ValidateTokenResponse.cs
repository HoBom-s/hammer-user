namespace Hammer.User.Application.UseCases.ValidateToken;

/// <summary>
///     Response DTO for a valid access token introspection.
/// </summary>
public sealed record ValidateTokenResponse(Guid UserId, string Email, string Nickname);
