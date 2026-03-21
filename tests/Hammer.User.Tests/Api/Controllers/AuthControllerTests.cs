using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hammer.User.Application.UseCases.Login;
using Hammer.User.Application.UseCases.Register;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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

    private static void ReplaceService<T>(IServiceCollection services, T? implementation)
        where T : class
    {
        if (implementation is null)
            return;

        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddScoped(_ => implementation);
    }

    private HttpClient CreateClient(
        IRegisterUserUseCase? registerUseCase = null,
        ILoginUserUseCase? loginUseCase = null)
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
                ReplaceService(services, registerUseCase);
                ReplaceService(services, loginUseCase);
            });
        }).CreateClient();
    }
}
