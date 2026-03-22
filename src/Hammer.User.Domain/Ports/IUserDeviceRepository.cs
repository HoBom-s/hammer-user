using Hammer.User.Domain.Entities;

namespace Hammer.User.Domain.Ports;

/// <summary>
/// Repository for user device lookups.
/// </summary>
public interface IUserDeviceRepository
{
    public Task<UserDevice?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
