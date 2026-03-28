using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Infrastructure.OAuth;

/// <summary>
///     Dispatches token validation to the appropriate provider client.
/// </summary>
internal sealed class OAuthUserInfoProvider : IOAuthUserInfoProvider
{
    private readonly Dictionary<OAuthProvider, IOAuthProviderClient> _clients;

    public OAuthUserInfoProvider(IEnumerable<IOAuthProviderClient> clients)
    {
        ArgumentNullException.ThrowIfNull(clients);
        _clients = clients.ToDictionary(c => c.Provider);
    }

    public Task<OAuthUserInfo> GetUserInfoAsync(OAuthProvider provider, string token, CancellationToken ct)
    {
        if (!_clients.TryGetValue(provider, out var client))
            throw new ArgumentOutOfRangeException(nameof(provider), provider, "지원하지 않는 OAuth 제공자입니다.");

        return client.GetUserInfoAsync(token, ct);
    }
}
