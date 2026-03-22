using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.RefreshToken;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.RefreshToken;

public sealed class RefreshTokenUseCaseTests
{
    private readonly RefreshTokenUseCase _sut;
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

    public RefreshTokenUseCaseTests()
    {
        var settings = Options.Create(new JwtSettings
        {
            Issuer = "hammer-user",
            Audience = "hammer",
            SecretKey = "test-secret-key-must-be-at-least-32-characters!!",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
        });

        _sut = new RefreshTokenUseCase(
            _refreshTokenRepository,
            _userRepository,
            _jwtTokenGenerator,
            settings);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRotateTokens_WhenTokenIsActive()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        var storedToken = Domain.Entities.RefreshToken.Create(
            user.Id,
            "old-token",
            DateTimeOffset.UtcNow.AddDays(7));

        _refreshTokenRepository.GetByTokenAsync("old-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);
        _jwtTokenGenerator.GenerateRefreshToken().Returns("new-refresh-token");
        _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.Nickname)
            .Returns("new-access-token");

        var result = await _sut.ExecuteAsync("old-token", CancellationToken.None);

        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
        storedToken.IsRevoked.Should().BeTrue();
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
    public async Task ExecuteAsync_ShouldThrow_WhenTokenIsExpired()
    {
        var storedToken = Domain.Entities.RefreshToken.Create(
            Guid.NewGuid(),
            "expired-token",
            DateTimeOffset.UtcNow.AddDays(-1));

        _refreshTokenRepository.GetByTokenAsync("expired-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        var act = () => _sut.ExecuteAsync("expired-token", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTokenIsRevoked()
    {
        var storedToken = Domain.Entities.RefreshToken.Create(
            Guid.NewGuid(),
            "revoked-token",
            DateTimeOffset.UtcNow.AddDays(7));
        storedToken.Revoke();

        _refreshTokenRepository.GetByTokenAsync("revoked-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);

        var act = () => _sut.ExecuteAsync("revoked-token", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserNotFound()
    {
        var storedToken = Domain.Entities.RefreshToken.Create(
            Guid.NewGuid(),
            "valid-token",
            DateTimeOffset.UtcNow.AddDays(7));

        _refreshTokenRepository.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(storedToken.UserId, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var act = () => _sut.ExecuteAsync("valid-token", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserIsSuspended()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        var storedToken = Domain.Entities.RefreshToken.Create(
            user.Id,
            "valid-token",
            DateTimeOffset.UtcNow.AddDays(7));

        // Suspend the user via reflection since there's no public method
        typeof(Domain.Entities.User)
            .GetProperty(nameof(Domain.Entities.User.Status))!
            .GetSetMethod(true)!
            .Invoke(user, [(object)UserStatus.Suspended]);

        _refreshTokenRepository.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var act = () => _sut.ExecuteAsync("valid-token", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
