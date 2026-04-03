namespace Hammer.User.Application.UseCases.GetDeviceToken;

/// <summary>
/// Use case contract for retrieving a user's device push token.
/// </summary>
public interface IGetDeviceTokenUseCase
{
    /// <summary>
    /// Returns the push token for the given user, or <c>null</c> if no device is registered.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The push token, or <c>null</c> if no device is registered.</returns>
    public Task<string?> ExecuteAsync(Guid userId, CancellationToken ct = default);
}
