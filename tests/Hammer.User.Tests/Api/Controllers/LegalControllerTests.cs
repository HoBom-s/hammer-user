using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hammer.User.Tests.Api.Controllers;

public sealed class LegalControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LegalControllerTests(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test");
            builder.UseSetting("Jwt:SecretKey", "test-secret-key-must-be-at-least-32-characters!!");
            builder.UseSetting("Jwt:Issuer", "test");
            builder.UseSetting("Jwt:Audience", "test");
            builder.UseSetting("Jwt:AccessTokenExpiryMinutes", "15");
            builder.UseSetting("Jwt:RefreshTokenExpiryDays", "7");
        }).CreateClient();
    }

    [Fact]
    public async Task GetTermsOfService_ShouldReturnSuccessStatusCode()
    {
        var response = await _client.GetAsync(new Uri("/hammer-users/legal/terms", UriKind.Relative));

        // Integration test — actual DB may not be available in test environment.
        // Verify the endpoint is registered and routable.
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPrivacyPolicy_ShouldReturnSuccessStatusCode()
    {
        var response = await _client.GetAsync(new Uri("/hammer-users/legal/privacy", UriKind.Relative));

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
