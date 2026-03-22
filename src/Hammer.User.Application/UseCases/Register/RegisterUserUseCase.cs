using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.Register;

/// <summary>
///     Registers a new user with email and password.
/// </summary>
internal sealed class RegisterUserUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    : IRegisterUserUseCase
{
    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken ct)
    {
        await CheckUserEmailAsync(request.Email, ct);

        var user = Domain.Entities.User.CreateWithCredentials(request.Email, request.Nickname, passwordHasher.Hash(request.Password));
        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);

        return new RegisterUserResponse(user.Id, user.Email!, user.Nickname);
    }

    private async Task CheckUserEmailAsync(string email, CancellationToken ct)
    {
        var existingUser = await userRepository.GetByEmailAsync(email, ct);

        if (existingUser is not null)
            throw new ConflictException("이미 존재하는 이메일입니다.");
    }
}
