using System.ComponentModel.DataAnnotations;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.UseCases.OAuthLogin;

/// <summary>
///     Request DTO for OAuth login.
/// </summary>
public sealed record OAuthLoginRequest(
    [Required] OAuthProvider Provider,
    [Required] string Token,
    string? Nickname);
