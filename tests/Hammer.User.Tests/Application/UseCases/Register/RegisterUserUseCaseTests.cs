using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.Register;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.Register;

public sealed class RegisterUserUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly RegisterUserUseCase _sut;

    public RegisterUserUseCaseTests()
    {
        _sut = new RegisterUserUseCase(_userRepository, _passwordHasher);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateUser_WhenEmailIsNew()
    {
        var request = CreateValidRequest();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);
        _passwordHasher.Hash(request.Password).Returns("hashed-password");

        var response = await _sut.ExecuteAsync(request, CancellationToken.None);

        response.Email.Should().Be(request.Email);
        response.Nickname.Should().Be(request.Nickname);
        response.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallAddAndSaveChanges()
    {
        var request = CreateValidRequest();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);
        _passwordHasher.Hash(request.Password).Returns("hashed-password");

        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHashPassword()
    {
        var request = CreateValidRequest();
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);
        _passwordHasher.Hash(request.Password).Returns("hashed-password");

        await _sut.ExecuteAsync(request, CancellationToken.None);

        _passwordHasher.Received(1).Hash(request.Password);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
    {
        var request = CreateValidRequest();
        var existingUser = Domain.Entities.User.CreateWithCredentials("test@example.com", "existing", "hash");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCallAdd_WhenEmailAlreadyExists()
    {
        var request = CreateValidRequest();
        var existingUser = Domain.Entities.User.CreateWithCredentials("test@example.com", "existing", "hash");
        _userRepository.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
    }

    private static RegisterUserRequest CreateValidRequest(string email = "test@example.com") =>
        new(email, "tester", "Test1234!");
}
