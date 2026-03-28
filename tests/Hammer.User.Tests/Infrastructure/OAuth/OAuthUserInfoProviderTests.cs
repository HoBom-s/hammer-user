using FluentAssertions;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Hammer.User.Infrastructure.OAuth;
using NSubstitute;

namespace Hammer.User.Tests.Infrastructure.OAuth;

public sealed class OAuthUserInfoProviderTests
{
    [Fact]
    public async Task GetUserInfoAsync_ShouldDispatchToCorrectClient()
    {
        var googleClient = Substitute.For<IOAuthProviderClient>();
        googleClient.Provider.Returns(OAuthProvider.Google);
        var expectedInfo = new OAuthUserInfo("sub-123", "test@gmail.com", "Test User");
        googleClient.GetUserInfoAsync("google-token", Arg.Any<CancellationToken>())
            .Returns(expectedInfo);

        var sut = new OAuthUserInfoProvider([googleClient]);

        var result = await sut.GetUserInfoAsync(OAuthProvider.Google, "google-token", CancellationToken.None);

        result.Should().Be(expectedInfo);
    }

    [Fact]
    public async Task GetUserInfoAsync_ShouldThrow_WhenProviderIsNotRegistered()
    {
        var googleClient = Substitute.For<IOAuthProviderClient>();
        googleClient.Provider.Returns(OAuthProvider.Google);
        var sut = new OAuthUserInfoProvider([googleClient]);

        var act = () => sut.GetUserInfoAsync(OAuthProvider.Kakao, "kakao-token", CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
