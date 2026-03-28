using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(HammerUserDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("동시 요청으로 인해 작업이 실패했습니다. 다시 시도해주세요.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("이미 존재하는 데이터입니다.");
        }
    }
}
