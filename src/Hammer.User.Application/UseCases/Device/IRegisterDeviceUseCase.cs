namespace Hammer.User.Application.UseCases.Device;

/// <summary>
///     Use case for registering a user's device.
/// </summary>
public interface IRegisterDeviceUseCase
{
    /// <summary>
    ///     Registers a device for the user, replacing any existing device.
    /// </summary>
    /// <param name="request">The device registration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ExecuteAsync(RegisterDeviceRequest request, CancellationToken ct);
}
