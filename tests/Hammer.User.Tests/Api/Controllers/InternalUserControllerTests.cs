using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.GetDeviceToken;
using Hammer.User.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Hammer.User.Tests.Api.Controllers;

public sealed class InternalUserControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public InternalUserControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDeviceToken_ShouldReturn200_WhenDeviceExists()
    {
        var deviceTokenUseCase = Substitute.For<IGetDeviceTokenUseCase>();
        deviceTokenUseCase.ExecuteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns("ExponentPushToken[abc123]");

        var userId = Guid.NewGuid();
        var client = CreateClient(deviceTokenUseCase: deviceTokenUseCase);

        var response = await client.GetAsync(
            new Uri($"/hammer-users/internal/users/{userId}/device-token", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DeviceTokenResponse>();
        body!.PushToken.Should().Be("ExponentPushToken[abc123]");
    }

    [Fact]
    public async Task GetDeviceToken_ShouldReturn404_WhenDeviceNotFound()
    {
        var deviceTokenUseCase = Substitute.For<IGetDeviceTokenUseCase>();
        deviceTokenUseCase.ExecuteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var userId = Guid.NewGuid();
        var client = CreateClient(deviceTokenUseCase: deviceTokenUseCase);

        var response = await client.GetAsync(
            new Uri($"/hammer-users/internal/users/{userId}/device-token", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClient(IGetDeviceTokenUseCase? deviceTokenUseCase = null)
    {
        deviceTokenUseCase ??= Substitute.For<IGetDeviceTokenUseCase>();

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
                services.ReplaceService(deviceTokenUseCase);
            });
        }).CreateClient();
    }
}
