using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.Login;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.Login;

public sealed class LoginUserUseCaseTests
{
    private readonly IOptions<JwtSettings> _jwtSettings = Options.Create(
        new JwtSettings
        {
            Issuer = "hammer-user",
            Audience = "hammer",
            SecretKey = "test-secret-key-that-is-long-enough-for-hmac",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
        });

    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private readonly LoginUserUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public LoginUserUseCaseTests()
    {
        _sut = new LoginUserUseCase(_userRepository, _passwordHasher, _jwtTokenGenerator, _jwtSettings);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        var request = new LoginUserRequest("test@example.com", "Test1234!");
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, "hashed").Returns(true);
        _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.Nickname).Returns("access-token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh-token");

        var response = await _sut.ExecuteAsync(request, CancellationToken.None);

        response.AccessToken.Should().Be("access-token");
        response.RefreshToken.Should().Be("refresh-token");
        response.RefreshTokenExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIssueRefreshTokenAndSave()
    {
        var request = new LoginUserRequest("test@example.com", "Test1234!");
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, "hashed").Returns(true);
        _jwtTokenGenerator.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns("at");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh-token");

        await _sut.ExecuteAsync(request, CancellationToken.None);

        user.RefreshTokens.Should().HaveCount(1);
        user.RefreshTokens.First().Token.Should().Be("refresh-token");
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserNotFound()
    {
        var request = new LoginUserRequest("no@example.com", "Test1234!");

        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenPasswordIsWrong()
    {
        var request = new LoginUserRequest("test@example.com", "Wrong1234!");
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, "hashed").Returns(false);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserHasNoPassword()
    {
        var request = new LoginUserRequest("oauth@example.com", "Test1234!");
        var user = Domain.Entities.User.CreateWithOAuth("oauth-user", "oauth@example.com", OAuthProvider.Google, "sub");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserIsDeleted()
    {
        var request = new LoginUserRequest("test@example.com", "Test1234!");
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        user.SoftDelete();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, Arg.Any<string>()).Returns(true);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowForbidden_WhenUserIsSuspended()
    {
        var request = new LoginUserRequest("test@example.com", "Test1234!");
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        user.Suspend();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, "hashed").Returns(true);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*정지*");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowUnauthorized_WhenPasswordIsWrongEvenIfSuspended()
    {
        var request = new LoginUserRequest("test@example.com", "Wrong!");
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        user.Suspend();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(request.Password, "hashed").Returns(false);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
