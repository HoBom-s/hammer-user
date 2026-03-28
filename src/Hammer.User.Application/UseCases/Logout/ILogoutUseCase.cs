namespace Hammer.User.Application.UseCases.Logout;

/// <summary>
///     Use case for logging out a user by revoking their refresh token and removing their device.
/// </summary>
public interface ILogoutUseCase
{
    /// <summary>
    ///     Revokes the refresh token and removes the associated user's device.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ExecuteAsync(string refreshToken, CancellationToken ct);
}
