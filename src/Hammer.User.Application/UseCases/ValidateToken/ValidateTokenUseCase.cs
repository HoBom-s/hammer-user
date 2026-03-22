using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.ValidateToken;

internal sealed class ValidateTokenUseCase(
    IJwtTokenGenerator jwtTokenGenerator,
    IUserRepository userRepository)
    : IValidateTokenUseCase
{
    private const string InvalidTokenMessage = "유효하지 않은 토큰이에요.";

    public async Task<ValidateTokenResponse> ExecuteAsync(ValidateTokenRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claims = jwtTokenGenerator.ValidateAccessToken(request.AccessToken)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        var user = await userRepository.GetByIdAsync(claims.UserId, ct)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        if (!user.IsActive)
            throw new UnauthorizedException(InvalidTokenMessage);

        if (user.Email is null)
            throw new UnauthorizedException(InvalidTokenMessage);

        return new ValidateTokenResponse(user.Id, user.Email, user.Nickname);
    }
}
