using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.GetUsers;
using Hammer.User.Domain.Enums;
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
    public async Task GetUsers_ShouldReturn200_WithPagedResponse()
    {
        var now = DateTimeOffset.UtcNow;
        var useCase = Substitute.For<IGetUsersUseCase>();
        useCase.ExecuteAsync(Arg.Any<GetUsersRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResponse<UserSummaryResponse>(
                [new UserSummaryResponse(Guid.NewGuid(), "test@example.com", "tester", UserStatus.Active, true, now, now)],
                1,
                20,
                1,
                1));

        var client = CreateClient(useCase);

        var response = await client.GetAsync(new Uri("/hammer-users/internal/users", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<UserSummaryResponse>>();
        body!.Items.Should().HaveCount(1);
        body.Page.Should().Be(1);
        body.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUsers_ShouldReturn200_WithDefaultPageAndSize()
    {
        var useCase = Substitute.For<IGetUsersUseCase>();
        useCase.ExecuteAsync(Arg.Any<GetUsersRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResponse<UserSummaryResponse>([], 1, 20, 0, 0));

        var client = CreateClient(useCase);

        var response = await client.GetAsync(new Uri("/hammer-users/internal/users", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await useCase.Received(1).ExecuteAsync(
            Arg.Is<GetUsersRequest>(r => r.Page == 1 && r.Size == 20 && r.Status == null),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(-1, 10)]
    [InlineData(1, 101)]
    public async Task GetUsers_ShouldReturn400_WhenPageOrSizeIsInvalid(int page, int size)
    {
        var useCase = Substitute.For<IGetUsersUseCase>();
        var client = CreateClient(useCase);

        var response = await client.GetAsync(
            new Uri($"/hammer-users/internal/users?page={page}&size={size}", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private HttpClient CreateClient(IGetUsersUseCase useCase)
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
