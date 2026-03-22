using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.ValidateToken;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.ValidateToken;

public sealed class ValidateTokenUseCaseTests
{
    private readonly ValidateTokenUseCase _sut;
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public ValidateTokenUseCaseTests()
    {
        _sut = new ValidateTokenUseCase(_jwtTokenGenerator, _userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnClaims_WhenTokenIsValidAndUserIsActive()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _jwtTokenGenerator.ValidateAccessToken("valid-token")
            .Returns(new AccessTokenClaims(user.Id, "test@example.com", "tester"));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new ValidateTokenRequest("valid-token");
        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be("test@example.com");
        result.Nickname.Should().Be("tester");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTokenIsInvalid()
    {
        _jwtTokenGenerator.ValidateAccessToken("bad-token")
            .Returns((AccessTokenClaims?)null);

        var request = new ValidateTokenRequest("bad-token");
        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        _jwtTokenGenerator.ValidateAccessToken("valid-token")
            .Returns(new AccessTokenClaims(userId, "test@example.com", "tester"));
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var request = new ValidateTokenRequest("valid-token");
        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUserIsSuspended()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        typeof(Domain.Entities.User)
            .GetProperty(nameof(Domain.Entities.User.Status))!
            .GetSetMethod(true)!
            .Invoke(user, [(object)UserStatus.Suspended]);

        _jwtTokenGenerator.ValidateAccessToken("valid-token")
            .Returns(new AccessTokenClaims(user.Id, "test@example.com", "tester"));
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new ValidateTokenRequest("valid-token");
        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
