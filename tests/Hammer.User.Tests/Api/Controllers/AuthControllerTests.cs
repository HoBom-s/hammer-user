using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.Login;
using Hammer.User.Application.UseCases.RefreshToken;
using Hammer.User.Application.UseCases.Register;
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

    private HttpClient CreateClient(
        IRegisterUserUseCase? registerUseCase = null,
        ILoginUserUseCase? loginUseCase = null,
        IRefreshTokenUseCase? refreshUseCase = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
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
            });
        }).CreateClient();
    }
}
