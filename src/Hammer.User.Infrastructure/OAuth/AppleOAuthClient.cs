using System.IdentityModel.Tokens.Jwt;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Hammer.User.Infrastructure.OAuth;

/// <summary>
///     Apple OAuth client — validates ID tokens via OIDC discovery + JWKS.
/// </summary>
internal sealed class AppleOAuthClient : IOAuthProviderClient
{
    private static readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager = new(
        "https://appleid.apple.com/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever());

    private readonly OAuthSettings.ProviderSettings _settings;

    public AppleOAuthClient(IOptions<OAuthSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value.Apple;
    }

    public OAuthProvider Provider => OAuthProvider.Apple;

    public async Task<OAuthUserInfo> GetUserInfoAsync(string token, CancellationToken ct)
    {
        var config = await _configManager.GetConfigurationAsync(ct);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://appleid.apple.com",
            ValidateAudience = true,
            ValidAudiences = _settings.ClientIds,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        };

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var result = await handler.ValidateTokenAsync(token, validationParameters);

        if (!result.IsValid)
            throw new SecurityTokenException("Apple ID token validation failed.");

        var sub = result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new SecurityTokenException("Apple ID token is missing 'sub' claim.");

        var email = result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

        return new OAuthUserInfo(sub, email, null);
    }
}
