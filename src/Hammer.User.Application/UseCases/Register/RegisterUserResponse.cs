namespace Hammer.User.Application.UseCases.Register;

/// <summary>
/// Response DTO for user registration.
/// </summary>
public sealed record RegisterUserResponse(Guid UserId, string Email, string Nickname);
