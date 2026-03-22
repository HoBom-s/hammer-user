using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(HammerUserDbContext context) : IUserRepository
{
    public async Task<Domain.Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<(IReadOnlyList<Domain.Entities.User> Items, int TotalCount)> GetPagedAsync(
        int page, int size, Domain.Enums.UserStatus? status = null, CancellationToken cancellationToken = default)
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
