using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.Common;

public sealed record UserInfoDeviceResponse(
    DevicePlatform Platform,
    string DeviceIdentifier,
    string FcmToken)
{
    public static UserInfoDeviceResponse FromEntity(UserDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        return new UserInfoDeviceResponse(device.Platform, device.DeviceIdentifier, device.FcmToken);
    }
}
