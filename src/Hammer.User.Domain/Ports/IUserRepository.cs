using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Ports;

/// <summary>
///     Repository for the User aggregate root.
/// </summary>
public interface IUserRepository
{
    public Task<Entities.User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    public Task<Entities.User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    public Task<(IReadOnlyList<Entities.User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int size,
        UserStatus? status = null,
        CancellationToken cancellationToken = default);

    public Task AddAsync(Entities.User user, CancellationToken cancellationToken = default);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
