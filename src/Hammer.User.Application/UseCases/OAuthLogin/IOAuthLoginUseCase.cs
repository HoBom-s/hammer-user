using Hammer.User.Application.UseCases.Login;

namespace Hammer.User.Application.UseCases.OAuthLogin;

/// <summary>
///     Use case for authenticating a user via OAuth provider.
/// </summary>
public interface IOAuthLoginUseCase
{
    /// <summary>
    ///     Validates the OAuth token and issues JWT tokens.
    /// </summary>
    /// <param name="request">The OAuth login request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The login response with access and refresh tokens.</returns>
    public Task<LoginUserResponse> ExecuteAsync(OAuthLoginRequest request, CancellationToken ct);
}
