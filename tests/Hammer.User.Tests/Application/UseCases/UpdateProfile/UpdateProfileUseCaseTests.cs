using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.UpdateProfile;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.UpdateProfile;

public sealed class UpdateProfileUseCaseTests
{
    private readonly UpdateProfileUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    public UpdateProfileUseCaseTests()
    {
        _sut = new UpdateProfileUseCase(_userRepository, _passwordHasher);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateNickname_WhenNicknameProvided()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "old-nick", "hashed");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateProfileRequest("new-nick", null, null);
        var response = await _sut.ExecuteAsync(user.Id, request, CancellationToken.None);

        response.Nickname.Should().Be("new-nick");
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldChangePassword_WhenCurrentPasswordMatches()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "old-hash");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("current-pass", "old-hash").Returns(true);
        _passwordHasher.Hash("new-pass").Returns("new-hash");

        var request = new UpdateProfileRequest(null, "current-pass", "new-pass");
        await _sut.ExecuteAsync(user.Id, request, CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowBadRequestException_WhenCurrentPasswordDoesNotMatch()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "old-hash");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong-pass", "old-hash").Returns(false);

        var request = new UpdateProfileRequest(null, "wrong-pass", "new-pass");
        var act = () => _sut.ExecuteAsync(user.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowBadRequestException_WhenCurrentPasswordMissing()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "old-hash");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateProfileRequest(null, null, "new-pass");
        var act = () => _sut.ExecuteAsync(user.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetPassword_WhenOAuthUserWithoutPassword()
    {
        var user = Domain.Entities.User.CreateWithOAuth(
            "tester",
            "test@example.com",
            Domain.Enums.OAuthProvider.Google,
            "google-sub-id");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Hash("new-pass").Returns("new-hash");

        var request = new UpdateProfileRequest(null, null, "new-pass");
        await _sut.ExecuteAsync(user.Id, request, CancellationToken.None);

        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _userRepository.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var request = new UpdateProfileRequest("nick", null, null);
        var act = () => _sut.ExecuteAsync(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowBadRequestException_WhenUserIsDeleted()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        user.SoftDelete();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var request = new UpdateProfileRequest("nick", null, null);
        var act = () => _sut.ExecuteAsync(user.Id, request, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }
}
