using System.Net.Http.Headers;
using System.Text.Json;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Infrastructure.OAuth;

/// <summary>
///     Naver OAuth client — validates access tokens via user info API.
/// </summary>
internal sealed class NaverOAuthClient(IHttpClientFactory httpClientFactory) : IOAuthProviderClient
{
    private static readonly Uri _userInfoUri = new("https://openapi.naver.com/v1/nid/me");

    public OAuthProvider Provider => OAuthProvider.Naver;

    public async Task<OAuthUserInfo> GetUserInfoAsync(string token, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, _userInfoUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            throw new UnauthorizedException("네이버 토큰이 유효하지 않습니다.");

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;

        if (!root.TryGetProperty("response", out var data))
            throw new UnauthorizedException("네이버 API 응답이 유효하지 않습니다.");

        var id = data.GetProperty("id").GetString()
            ?? throw new UnauthorizedException("네이버 API 응답에 id가 없습니다.");

        string? email = null;
        if (data.TryGetProperty("email", out var emailElement))
            email = emailElement.GetString();

        string? nickname = null;
        if (data.TryGetProperty("nickname", out var nicknameElement))
            nickname = nicknameElement.GetString();

        return new OAuthUserInfo(id, email, nickname);
    }
}
