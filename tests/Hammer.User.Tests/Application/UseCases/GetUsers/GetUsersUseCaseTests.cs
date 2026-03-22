using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.GetUsers;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.GetUsers;

public sealed class GetUsersUseCaseTests
{
    private readonly GetUsersUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public GetUsersUseCaseTests()
    {
        _sut = new GetUsersUseCase(_userRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPagedResponse_WhenUsersExist()
    {
        var user1 = Domain.Entities.User.CreateWithCredentials("a@example.com", "alice", "hash1");
        var user2 = Domain.Entities.User.CreateWithCredentials("b@example.com", "bob", "hash2");
        IReadOnlyList<Domain.Entities.User> items = [user1, user2];

        _userRepository.GetPagedAsync(1, 20, null, Arg.Any<CancellationToken>())
            .Returns((items, 2));

        var request = new GetUsersRequest();
        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.Size.Should().Be(20);
        result.TotalCount.Should().Be(2);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassStatusFilter_WhenStatusIsProvided()
    {
        IReadOnlyList<Domain.Entities.User> items = [];
        _userRepository.GetPagedAsync(1, 10, UserStatus.Active, Arg.Any<CancellationToken>())
            .Returns((items, 0));

        var request = new GetUsersRequest(1, 10, UserStatus.Active);
        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1)
            .GetPagedAsync(1, 10, UserStatus.Active, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenNoUsersMatch()
    {
        IReadOnlyList<Domain.Entities.User> items = [];
        _userRepository.GetPagedAsync(1, 20, null, Arg.Any<CancellationToken>())
            .Returns((items, 0));

        var request = new GetUsersRequest();
        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldConvertTimesToKst()
    {
        var user = Domain.Entities.User.CreateWithCredentials("a@example.com", "alice", "hash");
        IReadOnlyList<Domain.Entities.User> items = [user];

        _userRepository.GetPagedAsync(1, 20, null, Arg.Any<CancellationToken>())
            .Returns((items, 1));

        var request = new GetUsersRequest();
        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        var expectedCreatedAt = TimeZoneInfo.ConvertTime(user.CreatedAt, TimeZones.Korea);
        result.Items[0].CreatedAt.Should().Be(expectedCreatedAt);
        result.Items[0].CreatedAt.Offset.Should().Be(TimeSpan.FromHours(9));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCalculateTotalPages_WhenItemsSpanMultiplePages()
    {
        IReadOnlyList<Domain.Entities.User> items = [];
        _userRepository.GetPagedAsync(1, 10, null, Arg.Any<CancellationToken>())
            .Returns((items, 25));

        var request = new GetUsersRequest(1, 10);
        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        result.TotalPages.Should().Be(3);
    }
}
