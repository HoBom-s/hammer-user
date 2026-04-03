using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.Device;
using Hammer.User.Application.UseCases.Login;
using Hammer.User.Application.UseCases.Logout;
using Hammer.User.Application.UseCases.OAuthLogin;
using Hammer.User.Application.UseCases.RefreshToken;
using Hammer.User.Application.UseCases.Register;
using Hammer.User.Application.UseCases.UserInfo;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Hammer.User.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Hammer.User.Tests.Api.Controllers;

public sealed class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ShouldReturn201_WhenRequestIsValid()
    {
        var useCase = Substitute.For<IRegisterUserUseCase>();
        var expectedResponse = new RegisterUserResponse(Guid.NewGuid(), "test@example.com", "tester");
        useCase.ExecuteAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var client = CreateClient(registerUseCase: useCase);
        var request = new { Email = "test@example.com", Nickname = "tester", Password = "Test1234!" };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        body!.Email.Should().Be("test@example.com");
        body.Nickname.Should().Be("tester");
    }

    [Theory]
    [InlineData("", "tester", "Test1234!")]
    [InlineData("not-an-email", "tester", "Test1234!")]
    [InlineData("test@example.com", "", "Test1234!")]
    [InlineData("test@example.com", "tester", "")]
    [InlineData("test@example.com", "tester", "short1!")]
    [InlineData("test@example.com", "tester", "NoSpecial1")]
    [InlineData("test@example.com", "tester", "NoDigits!!")]
    public async Task Register_ShouldReturn400_WhenRequestIsInvalid(string email, string nickname, string password)
    {
        var client = CreateClient(registerUseCase: Substitute.For<IRegisterUserUseCase>());
        var request = new { Email = email, Nickname = nickname, Password = password };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturn200WithAccessToken_WhenCredentialsAreValid()
    {
        var loginUseCase = Substitute.For<ILoginUserUseCase>();
        loginUseCase.ExecuteAsync(Arg.Any<LoginUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LoginUserResponse("access-token", "refresh-token", DateTimeOffset.UtcNow.AddDays(7)));

        var client = CreateClient(loginUseCase: loginUseCase);
        var request = new { Email = "test@example.com", Password = "Test1234!" };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().Be("access-token");
    }

    [Fact]
    public async Task Login_ShouldSetRefreshTokenCookie_WhenCredentialsAreValid()
    {
        var loginUseCase = Substitute.For<ILoginUserUseCase>();
        loginUseCase.ExecuteAsync(Arg.Any<LoginUserRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LoginUserResponse("access-token", "refresh-token", DateTimeOffset.UtcNow.AddDays(7)));

        var client = CreateClient(loginUseCase: loginUseCase);
        var request = new { Email = "test@example.com", Password = "Test1234!" };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/login", request);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookieHeader = cookies!.First();
        cookieHeader.Should().Contain("refresh_token=refresh-token");
        cookieHeader.Should().Contain("httponly");
        cookieHeader.Should().Contain("samesite=strict");
        cookieHeader.Should().Contain("path=/hammer-users/auth");
    }

    [Theory]
    [InlineData("", "Test1234!")]
    [InlineData("not-an-email", "Test1234!")]
    [InlineData("test@example.com", "")]
    public async Task Login_ShouldReturn400_WhenRequestIsInvalid(string email, string password)
    {
        var client = CreateClient(loginUseCase: Substitute.For<ILoginUserUseCase>());
        var request = new { Email = email, Password = password };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_ShouldReturn200WithNewAccessToken_WhenCookieIsPresent()
    {
        var refreshUseCase = Substitute.For<IRefreshTokenUseCase>();
        refreshUseCase.ExecuteAsync("old-refresh-token", Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResponse(
                "new-access-token",
                "new-refresh-token",
                DateTimeOffset.UtcNow.AddDays(7)));

        var client = CreateClient(refreshUseCase: refreshUseCase);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hammer-users/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=old-refresh-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().Be("new-access-token");
    }

    [Fact]
    public async Task Refresh_ShouldSetRotatedRefreshTokenCookie()
    {
        var refreshUseCase = Substitute.For<IRefreshTokenUseCase>();
        refreshUseCase.ExecuteAsync("old-refresh-token", Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResponse(
                "new-access-token",
                "new-refresh-token",
                DateTimeOffset.UtcNow.AddDays(7)));

        var client = CreateClient(refreshUseCase: refreshUseCase);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hammer-users/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=old-refresh-token");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookieHeader = cookies!.First();
        cookieHeader.Should().Contain("refresh_token=new-refresh-token");
        cookieHeader.Should().Contain("httponly");
        cookieHeader.Should().Contain("path=/hammer-users/auth");
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WhenCookieIsMissing()
    {
        var client = CreateClient(refreshUseCase: Substitute.For<IRefreshTokenUseCase>());

        var response = await client.PostAsync(
            new Uri("/hammer-users/auth/refresh", UriKind.Relative),
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WhenUseCaseThrows()
    {
        var refreshUseCase = Substitute.For<IRefreshTokenUseCase>();
        refreshUseCase.ExecuteAsync("bad-token", Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedException("유효하지 않은 토큰이에요."));

        var client = CreateClient(refreshUseCase: refreshUseCase);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hammer-users/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=bad-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OAuthLogin_ShouldReturn200WithAccessToken_WhenValid()
    {
        var oAuthUseCase = Substitute.For<IOAuthLoginUseCase>();
        oAuthUseCase.ExecuteAsync(Arg.Any<OAuthLoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LoginUserResponse("oauth-access-token", "oauth-refresh-token", DateTimeOffset.UtcNow.AddDays(7)));

        var client = CreateClient(oAuthLoginUseCase: oAuthUseCase);
        var request = new { Provider = OAuthProvider.Google, Token = "google-token" };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/oauth", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().Be("oauth-access-token");
    }

    [Fact]
    public async Task OAuthLogin_ShouldSetRefreshTokenCookie_WhenValid()
    {
        var oAuthUseCase = Substitute.For<IOAuthLoginUseCase>();
        oAuthUseCase.ExecuteAsync(Arg.Any<OAuthLoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LoginUserResponse("oauth-access-token", "oauth-refresh-token", DateTimeOffset.UtcNow.AddDays(7)));

        var client = CreateClient(oAuthLoginUseCase: oAuthUseCase);
        var request = new { Provider = OAuthProvider.Google, Token = "google-token" };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/oauth", request);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookieHeader = cookies!.First();
        cookieHeader.Should().Contain("refresh_token=oauth-refresh-token");
        cookieHeader.Should().Contain("httponly");
        cookieHeader.Should().Contain("samesite=strict");
        cookieHeader.Should().Contain("path=/hammer-users/auth");
    }

    [Fact]
    public async Task OAuthLogin_ShouldReturn401_WhenUnauthorized()
    {
        var oAuthUseCase = Substitute.For<IOAuthLoginUseCase>();
        oAuthUseCase.ExecuteAsync(Arg.Any<OAuthLoginRequest>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedException("비활성화된 계정입니다."));

        var client = CreateClient(oAuthLoginUseCase: oAuthUseCase);
        var request = new { Provider = OAuthProvider.Google, Token = "google-token" };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/oauth", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OAuthLogin_ShouldReturn409_WhenEmailConflicts()
    {
        var oAuthUseCase = Substitute.For<IOAuthLoginUseCase>();
        oAuthUseCase.ExecuteAsync(Arg.Any<OAuthLoginRequest>(), Arg.Any<CancellationToken>())
            .Throws(new ConflictException("이미 해당 이메일로 가입된 계정이 존재합니다."));

        var client = CreateClient(oAuthLoginUseCase: oAuthUseCase);
        var request = new { Provider = OAuthProvider.Google, Token = "google-token" };

        var response = await client.PostAsJsonAsync("/hammer-users/auth/oauth", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpsertDevice_ShouldReturn200_WhenBearerTokenIsValid()
    {
        var registerDeviceUseCase = Substitute.For<IRegisterDeviceUseCase>();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtTokenGenerator.ValidateAccessToken("valid-access-token")
            .Returns(new AccessTokenClaims(Guid.NewGuid(), "test@example.com", "tester"));

        var client = CreateClient(registerDeviceUseCase: registerDeviceUseCase, jwtTokenGenerator: jwtTokenGenerator);
        using var request = new HttpRequestMessage(HttpMethod.Put, "/hammer-users/auth/device");
        request.Headers.Add("Authorization", "Bearer valid-access-token");
        request.Content = JsonContent.Create(new { Platform = DevicePlatform.Ios, DeviceIdentifier = "device-123", PushToken = "fcm-token" });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await registerDeviceUseCase.Received(1).ExecuteAsync(Arg.Any<RegisterDeviceRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertDevice_ShouldReturn401_WhenNoAuthHeader()
    {
        var client = CreateClient(registerDeviceUseCase: Substitute.For<IRegisterDeviceUseCase>(), jwtTokenGenerator: Substitute.For<IJwtTokenGenerator>());
        using var request = new HttpRequestMessage(HttpMethod.Put, "/hammer-users/auth/device");
        request.Content = JsonContent.Create(new { Platform = DevicePlatform.Ios, DeviceIdentifier = "device-123", PushToken = "fcm-token" });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpsertDevice_ShouldReturn401_WhenTokenIsInvalid()
    {
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtTokenGenerator.ValidateAccessToken("invalid-token").Returns((AccessTokenClaims?)null);

        var client = CreateClient(registerDeviceUseCase: Substitute.For<IRegisterDeviceUseCase>(), jwtTokenGenerator: jwtTokenGenerator);
        using var request = new HttpRequestMessage(HttpMethod.Put, "/hammer-users/auth/device");
        request.Headers.Add("Authorization", "Bearer invalid-token");
        request.Content = JsonContent.Create(new { Platform = DevicePlatform.Ios, DeviceIdentifier = "device-123", PushToken = "fcm-token" });

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturn204_WhenCookieIsPresent()
    {
        var logoutUseCase = Substitute.For<ILogoutUseCase>();
        var client = CreateClient(logoutUseCase: logoutUseCase);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hammer-users/auth/logout");
        request.Headers.Add("Cookie", "refresh_token=some-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await logoutUseCase.Received(1).ExecuteAsync("some-token", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_ShouldDeleteRefreshTokenCookie()
    {
        var logoutUseCase = Substitute.For<ILogoutUseCase>();
        var client = CreateClient(logoutUseCase: logoutUseCase);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hammer-users/auth/logout");
        request.Headers.Add("Cookie", "refresh_token=some-token");

        var response = await client.SendAsync(request);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        var cookieHeader = cookies!.First();
        cookieHeader.Should().Contain("refresh_token=");
        cookieHeader.Should().Contain("path=/hammer-users/auth");
        cookieHeader.Should().Contain("expires=");
    }

    [Fact]
    public async Task Logout_ShouldReturn401_WhenCookieIsMissing()
    {
        var client = CreateClient(logoutUseCase: Substitute.For<ILogoutUseCase>());

        var response = await client.PostAsync(
            new Uri("/hammer-users/auth/logout", UriKind.Relative),
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ShouldReturn200WithUserInfo_WhenTokenIsValid()
    {
        var userId = Guid.NewGuid();
        var jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        jwtTokenGenerator.ValidateAccessToken("valid-access-token")
            .Returns(new AccessTokenClaims(userId, "test@example.com", "tester"));

        var getUserInfoByTokenUseCase = Substitute.For<IGetUserInfoByTokenUseCase>();
        var expectedResponse = new UserInfoDetailResponse(
            userId,
            "test@example.com",
            "tester",
            UserStatus.Active,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        getUserInfoByTokenUseCase.ExecuteAsync(userId, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var client = CreateClient(getUserInfoByTokenUseCase: getUserInfoByTokenUseCase, jwtTokenGenerator: jwtTokenGenerator);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/hammer-users/auth/me");
        request.Headers.Add("Authorization", "Bearer valid-access-token");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(userId.ToString());
        body.GetProperty("nickname").GetString().Should().Be("tester");
    }

    [Fact]
    public async Task GetMe_ShouldReturn401_WhenNoAuthHeader()
    {
        var client = CreateClient(jwtTokenGenerator: Substitute.For<IJwtTokenGenerator>());

        var response = await client.GetAsync(new Uri("/hammer-users/auth/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private HttpClient CreateClient(
        IRegisterUserUseCase? registerUseCase = null,
        ILoginUserUseCase? loginUseCase = null,
        IRefreshTokenUseCase? refreshUseCase = null,
        IOAuthLoginUseCase? oAuthLoginUseCase = null,
        IRegisterDeviceUseCase? registerDeviceUseCase = null,
        ILogoutUseCase? logoutUseCase = null,
        IGetUserInfoByTokenUseCase? getUserInfoByTokenUseCase = null,
        IJwtTokenGenerator? jwtTokenGenerator = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test");
            builder.UseSetting("Jwt:SecretKey", "test-secret-key-must-be-at-least-32-characters!!");
            builder.UseSetting("Jwt:Issuer", "test");
            builder.UseSetting("Jwt:Audience", "test");
            builder.UseSetting("Jwt:AccessTokenExpiryMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenExpiryDays", "7");
            builder.ConfigureServices(services =>
            {
                if (registerUseCase is not null)
                    services.ReplaceService(registerUseCase);

                if (loginUseCase is not null)
                    services.ReplaceService(loginUseCase);

                if (refreshUseCase is not null)
                    services.ReplaceService(refreshUseCase);

                if (oAuthLoginUseCase is not null)
                    services.ReplaceService(oAuthLoginUseCase);

                if (registerDeviceUseCase is not null)
                    services.ReplaceService(registerDeviceUseCase);

                if (logoutUseCase is not null)
                    services.ReplaceService(logoutUseCase);

                if (getUserInfoByTokenUseCase is not null)
                    services.ReplaceService(getUserInfoByTokenUseCase);

                if (jwtTokenGenerator is not null)
                    services.ReplaceService(jwtTokenGenerator);
            });
        }).CreateClient();
    }
}
