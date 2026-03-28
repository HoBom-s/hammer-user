using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Infrastructure.OAuth;

/// <summary>
///     Kakao OAuth client — validates access tokens via user info API.
/// </summary>
internal sealed class KakaoOAuthClient(IHttpClientFactory httpClientFactory) : IOAuthProviderClient
{
    private static readonly Uri _userInfoUri = new("https://kapi.kakao.com/v2/user/me");

    public OAuthProvider Provider => OAuthProvider.Kakao;

    public async Task<OAuthUserInfo> GetUserInfoAsync(string token, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, _userInfoUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            throw new UnauthorizedException("카카오 토큰이 유효하지 않습니다.");

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;

        var id = root.GetProperty("id").GetInt64().ToString(CultureInfo.InvariantCulture);

        string? email = null;
        string? nickname = null;

        if (root.TryGetProperty("kakao_account", out var account))
        {
            if (account.TryGetProperty("email", out var emailElement))
                email = emailElement.GetString();

            if (account.TryGetProperty("profile", out var profile) && profile.TryGetProperty("nickname", out var nicknameElement))
                nickname = nicknameElement.GetString();
        }

        return new OAuthUserInfo(id, email, nickname);
    }
}
