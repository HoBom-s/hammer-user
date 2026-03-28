using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.UserInfo;
using Hammer.User.Domain.Enums;
using Hammer.User.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Hammer.User.Tests.Api.Controllers;

public sealed class UserControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetById_ShouldReturn200_WhenUserExists()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var useCase = Substitute.For<IGetUserInfoByIdUseCase>();
        useCase.ExecuteAsync(id, Arg.Any<CancellationToken>())
            .Returns(new UserSummaryResponse(
                id,
                "test@example.com",
                "tester",
                UserStatus.Active,
                true,
                now,
                now));

        var client = CreateClient(useCase);

        var response = await client.GetAsync(new Uri($"/hammer-users/users/{id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        body!.Id.Should().Be(id);
        body.Email.Should().Be("test@example.com");
        body.Nickname.Should().Be("tester");
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        var useCase = Substitute.For<IGetUserInfoByIdUseCase>();
        useCase.ExecuteAsync(id, Arg.Any<CancellationToken>())
            .Throws(new NotFoundException($"유저를 찾을 수 없습니다: {id}"));

        var client = CreateClient(useCase);

        var response = await client.GetAsync(new Uri($"/hammer-users/users/{id}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient CreateClient(IGetUserInfoByIdUseCase useCase)
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
                services.ReplaceService(useCase);
            });
        }).CreateClient();
    }
}
