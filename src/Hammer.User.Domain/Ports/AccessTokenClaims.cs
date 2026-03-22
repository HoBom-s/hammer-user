namespace Hammer.User.Domain.Ports;

/// <summary>
///     Parsed claims extracted from a validated access token.
/// </summary>
public sealed record AccessTokenClaims(Guid UserId, string Email, string Nickname);
