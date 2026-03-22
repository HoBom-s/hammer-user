using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(HammerUserDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
