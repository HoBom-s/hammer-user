using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.Options;

namespace Hammer.User.Application.UseCases.Login;

/// <summary>
///     Authenticates a user with email and password, issuing JWT tokens.
/// </summary>
internal sealed class LoginUserUseCase(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<JwtSettings> jwtSettings)
    : ILoginUserUseCase
{
    private const string InvalidCredentialsMessage = "이메일 또는 비밀번호가 올바르지 않습니다.";

    public async Task<LoginUserResponse> ExecuteAsync(LoginUserRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        if (!user.HasPassword())
            throw new UnauthorizedException(InvalidCredentialsMessage);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash!))
            throw new UnauthorizedException(InvalidCredentialsMessage);

        if (user.Status != UserStatus.Active)
            throw new UnauthorizedException(InvalidCredentialsMessage);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.Nickname);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays);

        user.IssueRefreshToken(refreshToken, refreshTokenExpiresAt);
        await userRepository.SaveChangesAsync(ct);

        return new LoginUserResponse(accessToken, refreshToken, refreshTokenExpiresAt);
    }
}
