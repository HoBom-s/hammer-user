using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.UseCases.Device;

/// <summary>
///     Request DTO for device registration.
/// </summary>
public sealed record RegisterDeviceRequest(Guid UserId, DevicePlatform Platform, string DeviceIdentifier, string FcmToken);
