using FluentAssertions;
using Hammer.User.Domain.Enums;
using UserEntity = Hammer.User.Domain.Entities.User;

namespace Hammer.User.Tests.Entities;

public sealed class UserTests
{
    [Fact]
    public void Suspend_ShouldTransitionToSuspended_WhenActive()
    {
        var user = NewActiveUser();

        user.Suspend();

        user.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public void Suspend_ShouldRevokeAllRefreshTokens_WhenTransitioning()
    {
        var user = NewActiveUser();
        user.IssueRefreshToken("rt-1", DateTimeOffset.UtcNow.AddDays(7));
        user.RefreshTokens.Single().IsRevoked.Should().BeFalse();

        user.Suspend();

        user.RefreshTokens.Single().IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Suspend_ShouldBeNoOp_WhenAlreadySuspended()
    {
        var user = NewActiveUser();
        user.Suspend();
        var snapshotUpdatedAt = user.UpdatedAt;

        user.Suspend();

        user.Status.Should().Be(UserStatus.Suspended);
        user.UpdatedAt.Should().Be(snapshotUpdatedAt);
    }

    [Fact]
    public void Suspend_ShouldThrow_WhenDeleted()
    {
        var user = NewActiveUser();
        user.SoftDelete();

        var act = () => user.Suspend();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Deleted user cannot be suspended.");
    }

    [Fact]
    public void Activate_ShouldTransitionToActive_WhenSuspended()
    {
        var user = NewActiveUser();
        user.Suspend();

        user.Activate();

        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void Activate_ShouldNotReissueRefreshTokens_WhenTransitioning()
    {
        var user = NewActiveUser();
        user.IssueRefreshToken("rt-1", DateTimeOffset.UtcNow.AddDays(7));
        user.Suspend();

        user.Activate();

        user.RefreshTokens.Single().IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Activate_ShouldBeNoOp_WhenAlreadyActive()
    {
        var user = NewActiveUser();
        var snapshotUpdatedAt = user.UpdatedAt;

        user.Activate();

        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAt.Should().Be(snapshotUpdatedAt);
    }

    [Fact]
    public void Activate_ShouldThrow_WhenDeleted()
    {
        var user = NewActiveUser();
        user.SoftDelete();

        var act = () => user.Activate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Deleted user cannot be activated.");
    }

    private static UserEntity NewActiveUser() =>
        UserEntity.CreateWithCredentials("test@example.com", "tester", "hashed");
}
