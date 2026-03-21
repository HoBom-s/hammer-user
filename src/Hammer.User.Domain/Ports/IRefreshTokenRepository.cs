using Hammer.User.Domain.Entities;

namespace Hammer.User.Domain.Ports;

/// <summary>
/// Repository for refresh token lookups.
/// </summary>
public interface IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
