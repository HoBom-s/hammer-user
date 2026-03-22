using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.Options;

namespace Hammer.User.Application.UseCases.RefreshToken;

internal sealed class RefreshTokenUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<JwtSettings> jwtSettings)
    : IRefreshTokenUseCase
{
    private const string InvalidTokenMessage = "유효하지 않은 토큰이에요.";

    public async Task<RefreshTokenResponse> ExecuteAsync(string refreshToken, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var storedToken = await refreshTokenRepository.GetByTokenAsync(refreshToken, ct)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        if (!storedToken.IsActive)
            throw new UnauthorizedException(InvalidTokenMessage);

        var user = await userRepository.GetByIdAsync(storedToken.UserId, ct)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        if (!user.IsActive)
            throw new UnauthorizedException(InvalidTokenMessage);

        storedToken.Revoke();

        var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();
        var newRefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays);
        user.IssueRefreshToken(newRefreshToken, newRefreshTokenExpiresAt);

        if (user.Email is null)
            throw new UnauthorizedException(InvalidTokenMessage);

        var newAccessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Nickname);

        await userRepository.SaveChangesAsync(ct);

        return new RefreshTokenResponse(newAccessToken, newRefreshToken, newRefreshTokenExpiresAt);
    }
}
