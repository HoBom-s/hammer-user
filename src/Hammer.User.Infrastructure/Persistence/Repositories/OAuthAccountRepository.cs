using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class OAuthAccountRepository(HammerUserDbContext context) : IOAuthAccountRepository
{
    public async Task<OAuthAccount?> GetByProviderAndSubjectAsync(OAuthProvider provider, string providerSubjectId, CancellationToken cancellationToken = default) =>
        await context.OAuthAccounts.FirstOrDefaultAsync(
            o => o.Provider == provider && o.ProviderSubjectId == providerSubjectId,
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
