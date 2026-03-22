using System.ComponentModel.DataAnnotations;

namespace Hammer.User.Application.UseCases.ValidateToken;

/// <summary>
///     Request DTO for access token introspection.
/// </summary>
public sealed record ValidateTokenRequest([Required] string AccessToken);
