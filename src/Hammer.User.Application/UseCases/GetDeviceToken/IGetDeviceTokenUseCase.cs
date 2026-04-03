namespace Hammer.User.Application.UseCases.GetDeviceToken;

/// <summary>
/// Use case contract for retrieving a user's device push token.
/// </summary>
public interface IGetDeviceTokenUseCase
{
    /// <summary>
    /// Returns the FCM token for the given user, or <c>null</c> if no device is registered.
    /// </summary>
    public Task<string?> ExecuteAsync(Guid userId, CancellationToken ct = default);
}
