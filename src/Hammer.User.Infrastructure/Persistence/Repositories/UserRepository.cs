using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(HammerUserDbContext context) : IUserRepository
{
    public async Task<Domain.Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(Domain.Entities.User user, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(user, cancellationToken);

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

    public async Task<IReadOnlyList<Domain.Entities.User>> GetDeletedUsersBeforeAsync(
        DateTimeOffset cutoff, int limit, CancellationToken cancellationToken = default) =>
        await context.Users
            .IgnoreQueryFilters()
            .Where(u => u.DeletedAt != null && u.DeletedAt < cutoff)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task HardDeleteAsync(Domain.Entities.User user, CancellationToken cancellationToken = default)
    {
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);
    }
}
