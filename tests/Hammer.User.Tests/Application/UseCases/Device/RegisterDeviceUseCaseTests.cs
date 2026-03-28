using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.Device;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.Device;

public sealed class RegisterDeviceUseCaseTests
{
    private readonly RegisterDeviceUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public RegisterDeviceUseCaseTests()
    {
        _sut = new RegisterDeviceUseCase(_userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRegisterDeviceAndSave()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        var request = new RegisterDeviceRequest(user.Id, DevicePlatform.Ios, "device-123", "fcm-token");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.ExecuteAsync(request, CancellationToken.None);

        user.Device.Should().NotBeNull();
        user.Device!.Platform.Should().Be(DevicePlatform.Ios);
        user.Device.DeviceIdentifier.Should().Be("device-123");
        user.Device.FcmToken.Should().Be("fcm-token");
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var request = new RegisterDeviceRequest(userId, DevicePlatform.Android, "device-123", "fcm-token");
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserIsInactive()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        user.SoftDelete();
        var request = new RegisterDeviceRequest(user.Id, DevicePlatform.Ios, "device-123", "fcm-token");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
