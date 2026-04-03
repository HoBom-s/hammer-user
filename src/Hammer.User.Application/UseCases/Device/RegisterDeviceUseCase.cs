using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.Device;

/// <summary>
///     Registers a device for a user, replacing any existing device (single-device policy).
/// </summary>
internal sealed class RegisterDeviceUseCase(IUserRepository userRepository) : IRegisterDeviceUseCase
{
    public async Task ExecuteAsync(RegisterDeviceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException($"유저를 찾을 수 없어요: {request.UserId}");

        if (!user.IsActive)
            throw new UnauthorizedException("비활성화된 계정입니다.");

        user.RegisterDevice(request.Platform, request.DeviceIdentifier, request.PushToken);
        await userRepository.SaveChangesAsync(ct);
    }
}
