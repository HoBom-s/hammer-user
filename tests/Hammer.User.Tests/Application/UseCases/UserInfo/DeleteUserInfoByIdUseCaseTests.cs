using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.UserInfo;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.UserInfo;

public sealed class DeleteUserInfoByIdUseCaseTests
{
    private readonly DeleteUserInfoByIdUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public DeleteUserInfoByIdUseCaseTests()
    {
        _sut = new DeleteUserInfoByIdUseCase(_userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnDeleteUserResponse_WhenUserExists()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var response = await _sut.ExecuteAsync(user.Id, CancellationToken.None);

        response.Id.Should().Be(user.Id);
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
    public async Task ExecuteAsync_ShouldThrowBadRequestException_WhenUserAlreadyDeleted()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        user.SoftDelete();
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => _sut.ExecuteAsync(user.Id, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallSaveChangesAsync_WhenUserIsDeleted()
    {
        var user = Domain.Entities.User.CreateWithCredentials("test@example.com", "tester", "hashed");
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.ExecuteAsync(user.Id, CancellationToken.None);

        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
