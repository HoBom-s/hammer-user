using System.ComponentModel.DataAnnotations;

namespace Hammer.User.Application.UseCases.Login;

/// <summary>
///     Request DTO for user login.
/// </summary>
public sealed record LoginUserRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password);
