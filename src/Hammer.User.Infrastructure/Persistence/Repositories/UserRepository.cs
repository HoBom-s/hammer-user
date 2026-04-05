using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Enums;
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

    public async Task<(IReadOnlyList<Domain.Entities.User> Items, int TotalCount)> GetPagedAsync(
        int page, int size, UserStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsQueryable();

        if (status.HasValue)
            query = query.Where(u => u.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

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
}
