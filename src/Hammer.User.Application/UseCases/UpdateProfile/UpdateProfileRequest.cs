namespace Hammer.User.Application.UseCases.UpdateProfile;

/// <summary>
///     Request DTO for profile update.
/// </summary>
/// <param name="Nickname">The new nickname, or <c>null</c> to keep unchanged.</param>
/// <param name="CurrentPassword">The current password for verification. Required when changing password for users with existing password.</param>
/// <param name="NewPassword">The new password, or <c>null</c> to keep unchanged.</param>
public sealed record UpdateProfileRequest(
    string? Nickname,
    string? CurrentPassword,
    string? NewPassword);
