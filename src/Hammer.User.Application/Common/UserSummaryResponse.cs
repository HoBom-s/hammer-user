using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.Common;

/// <summary>
///     Shared read-model DTO for user summary information.
/// </summary>
public sealed record UserSummaryResponse(
    Guid Id,
    string? Email,
    string Nickname,
    UserStatus Status,
    bool HasPassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static UserSummaryResponse FromEntity(Domain.Entities.User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserSummaryResponse(
            user.Id,
            user.Email,
            user.Nickname,
            user.Status,
            user.HasPassword(),
            TimeZoneInfo.ConvertTime(user.CreatedAt, TimeZones.Korea),
            TimeZoneInfo.ConvertTime(user.UpdatedAt, TimeZones.Korea));
    }
}
