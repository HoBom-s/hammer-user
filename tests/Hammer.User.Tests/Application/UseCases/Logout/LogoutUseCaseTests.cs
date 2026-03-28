using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.Logout;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.Logout;

public sealed class LogoutUseCaseTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly LogoutUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public LogoutUseCaseTests()
    {
        _sut = new LogoutUseCase(_refreshTokenRepository, _userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRevokeTokenAndRemoveDevice()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        var token = Domain.Entities.RefreshToken.Create(user.Id, "valid-token", DateTimeOffset.UtcNow.AddDays(7));

        _refreshTokenRepository.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.ExecuteAsync("valid-token", CancellationToken.None);

        token.IsRevoked.Should().BeTrue();
        user.Device.Should().BeNull();
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTokenNotFound()
    {
        _refreshTokenRepository.GetByTokenAsync("unknown", Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.RefreshToken?)null);

        var act = () => _sut.ExecuteAsync("unknown", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTokenIsRevoked()
    {
        var token = Domain.Entities.RefreshToken.Create(Guid.NewGuid(), "revoked-token", DateTimeOffset.UtcNow.AddDays(7));
        token.Revoke();

        _refreshTokenRepository.GetByTokenAsync("revoked-token", Arg.Any<CancellationToken>()).Returns(token);

        var act = () => _sut.ExecuteAsync("revoked-token", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTokenIsExpired()
    {
        var token = Domain.Entities.RefreshToken.Create(Guid.NewGuid(), "expired-token", DateTimeOffset.UtcNow.AddDays(-1));

        _refreshTokenRepository.GetByTokenAsync("expired-token", Arg.Any<CancellationToken>()).Returns(token);

        var act = () => _sut.ExecuteAsync("expired-token", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserNotFound()
    {
        var token = Domain.Entities.RefreshToken.Create(Guid.NewGuid(), "valid-token", DateTimeOffset.UtcNow.AddDays(7));

        _refreshTokenRepository.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(token);

        _userRepository.GetByIdAsync(token.UserId, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var act = () => _sut.ExecuteAsync("valid-token", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
