using FluentAssertions;
using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Ports;
using Hammer.User.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hammer.User.Tests.Infrastructure.Services;

public sealed class DeletedUserCleanupServiceTests : IDisposable
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly DeletedUserCleanupService _sut;

    public DeletedUserCleanupServiceTests()
    {
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(serviceProvider);
        serviceProvider.GetService(typeof(IUserRepository)).Returns(_repository);

        _sut = new DeletedUserCleanupService(scopeFactory, NullLogger<DeletedUserCleanupService>.Instance);
    }

    [Fact]
    public async Task Should_HardDelete_UsersDeletedMoreThan7DaysAgo()
    {
        var expiredUser = Domain.Entities.User.CreateWithCredentials("test@test.com", "nick", "hash");
        expiredUser.SoftDelete();

        _repository.GetDeletedUsersBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([expiredUser], Array.Empty<Domain.Entities.User>());

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await _sut.StopAsync(CancellationToken.None);

        await _repository.Received(1).HardDeleteAsync(expiredUser, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotHardDelete_WhenNoExpiredUsersExist()
    {
        _repository.GetDeletedUsersBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Entities.User>());

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await _sut.StopAsync(CancellationToken.None);

        await _repository.DidNotReceive().HardDeleteAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PassCorrectCutoffDate()
    {
        var before = DateTimeOffset.UtcNow - TimeSpan.FromDays(7);

        _repository.GetDeletedUsersBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Entities.User>());

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await _sut.StopAsync(CancellationToken.None);

        var after = DateTimeOffset.UtcNow - TimeSpan.FromDays(7);

        await _repository.Received(1).GetDeletedUsersBeforeAsync(
            Arg.Is<DateTimeOffset>(d => d >= before && d <= after),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ContinueRunning_WhenExceptionOccurs()
    {
        _repository.GetDeletedUsersBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Domain.Entities.User>>(_ => throw new InvalidOperationException("DB error"));

        using var cts = new CancellationTokenSource();

        var act = async () =>
        {
            await _sut.StartAsync(cts.Token);
            await Task.Delay(200);
            await cts.CancelAsync();
            await _sut.StopAsync(CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_DeleteInBatches_UntilNoneRemain()
    {
        var user1 = Domain.Entities.User.CreateWithCredentials("a@test.com", "a", "hash");
        user1.SoftDelete();
        var user2 = Domain.Entities.User.CreateWithCredentials("b@test.com", "b", "hash");
        user2.SoftDelete();

        _repository.GetDeletedUsersBeforeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([user1, user2], Array.Empty<Domain.Entities.User>());

        using var cts = new CancellationTokenSource();
        await _sut.StartAsync(cts.Token);
        await Task.Delay(200);
        await cts.CancelAsync();
        await _sut.StopAsync(CancellationToken.None);

        await _repository.Received(2).GetDeletedUsersBeforeAsync(
            Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).HardDeleteAsync(user1, Arg.Any<CancellationToken>());
        await _repository.Received(1).HardDeleteAsync(user2, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CalculateDelayUntilNextMidnightKst_ShouldReturnPositiveDelay()
    {
        var delay = DeletedUserCleanupService.CalculateDelayUntilNextMidnightKst();

        delay.Should().BeGreaterThan(TimeSpan.Zero);
        delay.Should().BeLessThanOrEqualTo(TimeSpan.FromHours(24));
    }

    public void Dispose() => _sut.Dispose();
}
