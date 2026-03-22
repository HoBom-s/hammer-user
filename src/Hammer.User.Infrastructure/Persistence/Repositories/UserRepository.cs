using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(HammerUserDbContext context) : IUserRepository
{
    public async Task<Domain.Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users.FindAsync([id], cancellationToken);

    public async Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(Domain.Entities.User user, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
