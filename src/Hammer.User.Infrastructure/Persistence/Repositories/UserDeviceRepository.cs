using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class UserDeviceRepository(HammerUserDbContext context) : IUserDeviceRepository
{
    public async Task<UserDevice?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.UserDevices.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
