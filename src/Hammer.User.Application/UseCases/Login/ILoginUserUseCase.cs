namespace Hammer.User.Application.UseCases.Login;

/// <summary>
///     Use case for authenticating a user with email and password.
/// </summary>
public interface ILoginUserUseCase
{
    public Task<LoginUserResponse> ExecuteAsync(LoginUserRequest request, CancellationToken ct);
}
