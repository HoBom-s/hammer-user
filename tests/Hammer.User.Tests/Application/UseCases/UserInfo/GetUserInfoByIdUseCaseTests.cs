using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.UserInfo;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.UserInfo;

public sealed class GetUserInfoByIdUseCaseTests
{
    private readonly GetUserInfoByIdUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public GetUserInfoByIdUseCaseTests()
    {
        _sut = new GetUserInfoByIdUseCase(_userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnUserInfo_WhenUserExists()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var response = await _sut.ExecuteAsync(user.Id, CancellationToken.None);

        response.Id.Should().Be(user.Id);
        response.Email.Should().Be("test@example.com");
        response.Nickname.Should().Be("tester");
        response.Status.Should().Be(Domain.Enums.UserStatus.Active);
        response.HasPassword.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _userRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var act = () => _sut.ExecuteAsync(id, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldConvertTimesToKst()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var response = await _sut.ExecuteAsync(user.Id, CancellationToken.None);

        var expectedCreatedAt = TimeZoneInfo.ConvertTime(user.CreatedAt, TimeZones.Korea);
        response.CreatedAt.Should().Be(expectedCreatedAt);
        response.CreatedAt.Offset.Should().Be(TimeSpan.FromHours(9));
    }
}
