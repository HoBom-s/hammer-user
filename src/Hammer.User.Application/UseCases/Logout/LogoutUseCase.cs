using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.Logout;

/// <summary>
///     Revokes the refresh token and removes the user's device.
/// </summary>
internal sealed class LogoutUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository) : ILogoutUseCase
{
    public async Task ExecuteAsync(string refreshToken, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var storedToken = await refreshTokenRepository.GetByTokenAsync(refreshToken, ct)
            ?? throw new UnauthorizedException("유효하지 않은 토큰이에요.");

        if (!storedToken.IsActive)
            throw new UnauthorizedException("유효하지 않은 토큰이에요.");

        var user = await userRepository.GetByIdAsync(storedToken.UserId, ct)
            ?? throw new UnauthorizedException("사용자를 찾을 수 없어요.");

        storedToken.Revoke();
        user.RemoveDevice();
        await userRepository.SaveChangesAsync(ct);
    }
}
