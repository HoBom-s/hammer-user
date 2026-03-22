using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.ValidateToken;
using Hammer.User.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Hammer.User.Tests.Api.Controllers;

public sealed class InternalAuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public InternalAuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Validate_ShouldReturn200WithClaims_WhenTokenIsValid()
    {
        var userId = Guid.NewGuid();
        var useCase = Substitute.For<IValidateTokenUseCase>();
        useCase.ExecuteAsync(Arg.Any<ValidateTokenRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidateTokenResponse(userId, "test@example.com", "tester"));

        var client = CreateClient(useCase);
        var request = new { AccessToken = "valid-token" };

        var response = await client.PostAsJsonAsync("/hammer-users/internal/auth/validate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ValidateTokenResponse>();
        body!.UserId.Should().Be(userId);
        body.Email.Should().Be("test@example.com");
        body.Nickname.Should().Be("tester");
    }

    [Fact]
    public async Task Validate_ShouldReturn401_WhenUseCaseThrows()
    {
        var useCase = Substitute.For<IValidateTokenUseCase>();
        useCase.ExecuteAsync(Arg.Any<ValidateTokenRequest>(), Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedException("유효하지 않은 토큰이에요."));

        var client = CreateClient(useCase);
        var request = new { AccessToken = "bad-token" };

        var response = await client.PostAsJsonAsync("/hammer-users/internal/auth/validate", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Validate_ShouldReturn400_WhenAccessTokenIsMissing()
    {
        var useCase = Substitute.For<IValidateTokenUseCase>();
        var client = CreateClient(useCase);

        var response = await client.PostAsJsonAsync(
            "/hammer-users/internal/auth/validate",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private HttpClient CreateClient(IValidateTokenUseCase useCase)
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
                services.ReplaceService(useCase);
            });
        }).CreateClient();
    }
}
