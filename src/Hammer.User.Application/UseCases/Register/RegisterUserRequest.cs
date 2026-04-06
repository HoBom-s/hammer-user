using System.ComponentModel.DataAnnotations;

namespace Hammer.User.Application.UseCases.Register;

/// <summary>
///     Request DTO for user registration.
/// </summary>
public sealed record RegisterUserRequest(
    [Required][EmailAddress] string Email,
    [Required] string Nickname,
    [Required]
    [MinLength(8)]
    [RegularExpression(
        @"^(?=.*[a-zA-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        ErrorMessage = "비밀번호는 영문, 숫자, 특수문자를 각각 하나 이상 포함해야 합니다.")]
    string Password,
    [Required] bool AgreeToTerms);
