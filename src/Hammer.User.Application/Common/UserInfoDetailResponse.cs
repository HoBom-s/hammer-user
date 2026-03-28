using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.Common;

public sealed record UserInfoDetailResponse(
    Guid Id,
    string? Email,
    string Nickname,
    UserStatus Status,
    UserInfoDeviceResponse? DeviceInfo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static UserInfoDetailResponse FromEntity(Domain.Entities.User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserInfoDetailResponse(
            user.Id,
            user.Email,
            user.Nickname,
            user.Status,
            user.Device is not null ? UserInfoDeviceResponse.FromEntity(user.Device) : null,
            TimeZoneInfo.ConvertTime(user.CreatedAt, TimeZones.Korea),
            TimeZoneInfo.ConvertTime(user.UpdatedAt, TimeZones.Korea));
    }
}
