using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Hammer.User.Tests.Infrastructure.Services;

public sealed class JwtTokenGeneratorTests
{
    private readonly JwtSettings _settings = new()
    {
        Issuer = "hammer-user",
        Audience = "hammer",
        SecretKey = "this-is-a-test-secret-key-that-is-at-least-32-bytes-long!",
        AccessTokenExpiryMinutes = 15,
        RefreshTokenExpiryDays = 7,
    };

    private readonly JwtTokenGenerator _sut;

    public JwtTokenGeneratorTests()
    {
        _sut = new JwtTokenGenerator(Options.Create(_settings));
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "test@example.com", "tester");

        token.Should().NotBeNullOrWhiteSpace();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainCorrectClaims()
    {
        var userId = Guid.NewGuid();
        var token = _sut.GenerateAccessToken(userId, "test@example.com", "tester");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("hammer-user");
        jwt.Audiences.Should().Contain("hammer");
        jwt.Subject.Should().Be(userId.ToString());
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("test@example.com");
        jwt.Claims.First(c => c.Type == "nickname").Value.Should().Be("tester");
    }

    [Fact]
    public void GenerateAccessToken_ShouldBeValidatableWithSigningKey()
    {
        var token = _sut.GenerateAccessToken(Guid.NewGuid(), "test@example.com", "tester");

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateLifetime = true,
        };

        var act = () => handler.ValidateToken(token, validationParameters, out _);

        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var token = _sut.GenerateRefreshToken();

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnDifferentValuesEachCall()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateAccessToken_ShouldReturnClaims_WhenTokenIsValid()
    {
        var userId = Guid.NewGuid();
        var token = _sut.GenerateAccessToken(userId, "test@example.com", "tester");

        var claims = _sut.ValidateAccessToken(token);

        claims.Should().NotBeNull();
        claims!.UserId.Should().Be(userId);
        claims.Email.Should().Be("test@example.com");
        claims.Nickname.Should().Be("tester");
    }

    [Fact]
    public void ValidateAccessToken_ShouldReturnNull_WhenTokenIsExpired()
    {
        var expiredSettings = new JwtSettings
        {
            Issuer = "hammer-user",
            Audience = "hammer",
            SecretKey = _settings.SecretKey,
            AccessTokenExpiryMinutes = -1,
            RefreshTokenExpiryDays = 7,
        };
        var expiredGenerator = new JwtTokenGenerator(Options.Create(expiredSettings));
        var token = expiredGenerator.GenerateAccessToken(Guid.NewGuid(), "test@example.com", "tester");

        var claims = _sut.ValidateAccessToken(token);

        claims.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_ShouldReturnNull_WhenSignatureIsInvalid()
    {
        var wrongKeySettings = new JwtSettings
        {
            Issuer = "hammer-user",
            Audience = "hammer",
            SecretKey = "a-completely-different-secret-key-that-is-long-enough!!",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
        };
        var wrongKeyGenerator = new JwtTokenGenerator(Options.Create(wrongKeySettings));
        var token = wrongKeyGenerator.GenerateAccessToken(Guid.NewGuid(), "test@example.com", "tester");

        var claims = _sut.ValidateAccessToken(token);

        claims.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_ShouldReturnNull_WhenTokenIsMalformed()
    {
        var claims = _sut.ValidateAccessToken("not-a-jwt-token");

        claims.Should().BeNull();
    }
}
