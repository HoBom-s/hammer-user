using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Ports;

/// <summary>
/// Repository for OAuth account lookups.
/// </summary>
public interface IOAuthAccountRepository
{
    public Task<OAuthAccount?> GetByProviderAndSubjectAsync(OAuthProvider provider, string providerSubjectId, CancellationToken cancellationToken = default);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
