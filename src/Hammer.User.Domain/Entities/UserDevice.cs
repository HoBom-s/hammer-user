using Hammer.User.Domain.Common;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Entities;

/// <summary>
/// Represents a user's registered device with its FCM push token.
/// </summary>
public sealed class UserDevice : Entity
{
    private UserDevice()
    {
    }

    /// <summary>
    /// Gets the owning user's identifier.
    /// </summary>
    public Guid UserId { get; private init; }

    /// <summary>
    /// Gets the device platform.
    /// </summary>
    public DevicePlatform Platform { get; private init; }

    /// <summary>
    /// Gets the unique device identifier.
    /// </summary>
    public string DeviceIdentifier { get; private init; } = string.Empty;

    /// <summary>
    /// Gets the Firebase Cloud Messaging token.
    /// </summary>
    public string FcmToken { get; private set; } = string.Empty;

    /// <summary>
    /// Creates a new <see cref="UserDevice"/>.
    /// </summary>
    /// <param name="userId">The owning user's identifier.</param>
    /// <param name="platform">The device platform.</param>
    /// <param name="deviceIdentifier">The unique device identifier.</param>
    /// <param name="fcmToken">The FCM push token.</param>
    /// <returns>A new <see cref="UserDevice"/> instance.</returns>
    public static UserDevice Create(Guid userId, DevicePlatform platform, string deviceIdentifier, string fcmToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(fcmToken);

        return new UserDevice
        {
            UserId = userId,
            Platform = platform,
            DeviceIdentifier = deviceIdentifier,
            FcmToken = fcmToken,
        };
    }

    /// <summary>
    /// Updates the FCM push token for this device.
    /// </summary>
    /// <param name="fcmToken">The new FCM token.</param>
    public void UpdateFcmToken(string fcmToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fcmToken);

        FcmToken = fcmToken;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
