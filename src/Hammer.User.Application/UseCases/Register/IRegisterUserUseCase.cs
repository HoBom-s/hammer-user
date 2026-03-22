namespace Hammer.User.Application.UseCases.Register;

/// <summary>
/// Use case for registering a new user with email and password.
/// </summary>
public interface IRegisterUserUseCase
{
    public Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken ct);
}
