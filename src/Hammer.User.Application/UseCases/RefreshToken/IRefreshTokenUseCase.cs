namespace Hammer.User.Application.UseCases.RefreshToken;

public interface IRefreshTokenUseCase
{
    public Task<RefreshTokenResponse> ExecuteAsync(string refreshToken, CancellationToken ct);
}
