using FluentAssertions;
using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.OAuthLogin;
using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hammer.User.Tests.Application.UseCases.OAuthLogin;

public sealed class OAuthLoginUseCaseTests
{
    private readonly IOptions<JwtSettings> _jwtSettings = Options.Create(
        new JwtSettings
        {
            Issuer = "hammer-user",
            Audience = "hammer",
            SecretKey = "test-secret-key-that-is-long-enough-for-hmac",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7,
        });

    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ILegalDocumentRepository _legalDocumentRepository = Substitute.For<ILegalDocumentRepository>();
    private readonly IOAuthAccountRepository _oAuthAccountRepository = Substitute.For<IOAuthAccountRepository>();
    private readonly IOAuthUserInfoProvider _oAuthUserInfoProvider = Substitute.For<IOAuthUserInfoProvider>();
    private readonly OAuthLoginUseCase _sut;
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    public OAuthLoginUseCaseTests()
    {
        var terms = LegalDocument.Create(LegalDocumentType.TermsOfService, "1.0", DateTimeOffset.UtcNow, "content");
        _legalDocumentRepository.GetLatestByTypeAsync(Arg.Any<LegalDocumentType>(), Arg.Any<CancellationToken>()).Returns(terms);

        _sut = new OAuthLoginUseCase(
            _oAuthUserInfoProvider,
            _oAuthAccountRepository,
            _userRepository,
            _jwtTokenGenerator,
            _jwtSettings,
            _legalDocumentRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTokens_WhenExistingOAuthUserLogsIn()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, null);
        var userInfo = new OAuthUserInfo("google-sub-123", "test@example.com", "Google User");
        var user = Domain.Entities.User.CreateWithOAuth("tester", "test@example.com", OAuthProvider.Google, "google-sub-123");
        var oAuthAccount = OAuthAccount.Create(user.Id, OAuthProvider.Google, "google-sub-123");

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-123", Arg.Any<CancellationToken>())
            .Returns(oAuthAccount);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);
        _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.Nickname).Returns("access-token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh-token");

        var response = await _sut.ExecuteAsync(request, CancellationToken.None);

        response.AccessToken.Should().Be("access-token");
        response.RefreshToken.Should().Be("refresh-token");
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenExistingOAuthUserIsInactive()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, null);
        var userInfo = new OAuthUserInfo("google-sub-123", "test@example.com", null);
        var user = Domain.Entities.User.CreateWithOAuth("tester", "test@example.com", OAuthProvider.Google, "google-sub-123");
        user.SoftDelete();
        var oAuthAccount = OAuthAccount.Create(user.Id, OAuthProvider.Google, "google-sub-123");

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-123", Arg.Any<CancellationToken>())
            .Returns(oAuthAccount);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenExistingOAuthUserNotFound()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, null);
        var userInfo = new OAuthUserInfo("google-sub-123", "test@example.com", null);
        var oAuthAccount = OAuthAccount.Create(Guid.NewGuid(), OAuthProvider.Google, "google-sub-123");

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-123", Arg.Any<CancellationToken>())
            .Returns(oAuthAccount);
        _userRepository.GetByIdAsync(oAuthAccount.UserId, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRegisterNewUser_WhenAccountDoesNotExistWithEmail()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, true);
        var userInfo = new OAuthUserInfo("google-sub-123", "new@example.com", "Google User");

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-123", Arg.Any<CancellationToken>())
            .Returns((OAuthAccount?)null);
        _userRepository.GetByEmailAsync("new@example.com", Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.User?)null);
        _jwtTokenGenerator.GenerateAccessToken(Arg.Any<Guid>(), "new@example.com", Arg.Any<string>()).Returns("access-token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh-token");

        var response = await _sut.ExecuteAsync(request, CancellationToken.None);

        response.AccessToken.Should().Be("access-token");
        await _userRepository.Received(1).AddAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRegisterNewUser_WhenEmailIsNull()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Kakao, "kakao-token", "카카오유저", true);
        var userInfo = new OAuthUserInfo("kakao-sub-123", null, null);

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Kakao, "kakao-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Kakao, "kakao-sub-123", Arg.Any<CancellationToken>())
            .Returns((OAuthAccount?)null);
        _jwtTokenGenerator.GenerateAccessToken(Arg.Any<Guid>(), string.Empty, "카카오유저").Returns("access-token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh-token");

        var response = await _sut.ExecuteAsync(request, CancellationToken.None);

        response.AccessToken.Should().Be("access-token");
        await _userRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.User>(u => u.Email == null && u.Nickname == "카카오유저"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowConflict_WhenEmailAlreadyExists()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, true);
        var userInfo = new OAuthUserInfo("google-sub-new", "existing@example.com", null);
        var existingUser = Domain.Entities.User.CreateWithCredentials("existing@example.com", "existing", "hashed");

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-new", Arg.Any<CancellationToken>())
            .Returns((OAuthAccount?)null);
        _userRepository.GetByEmailAsync("existing@example.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseRequestNickname_WhenProvided()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", "custom-nick", true);
        var userInfo = new OAuthUserInfo("google-sub-123", "new@example.com", "Provider Nickname");

        SetupNewUserMocks(userInfo);

        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.User>(u => u.Nickname == "custom-nick"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseProviderNickname_WhenRequestNicknameIsNull()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, true);
        var userInfo = new OAuthUserInfo("google-sub-123", "new@example.com", "Provider Nick");

        SetupNewUserMocks(userInfo);

        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.User>(u => u.Nickname == "Provider Nick"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseEmailLocalPart_WhenNicknamesAreNull()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, true);
        var userInfo = new OAuthUserInfo("google-sub-123", "localpart@example.com", null);

        SetupNewUserMocks(userInfo);

        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.User>(u => u.Nickname == "localpart"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateRandomNickname_WhenAllNicknameSourcesAreNull()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Kakao, "kakao-token", null, true);
        var userInfo = new OAuthUserInfo("kakao-sub-123", null, null);

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Kakao, "kakao-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Kakao, "kakao-sub-123", Arg.Any<CancellationToken>())
            .Returns((OAuthAccount?)null);
        _jwtTokenGenerator.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns("at");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("rt");

        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.User>(u => u.Nickname.StartsWith("user_", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallSaveChanges_WhenNewUserIsRegistered()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", "nick", true);
        var userInfo = new OAuthUserInfo("google-sub-123", "new@example.com", null);

        SetupNewUserMocks(userInfo);

        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(Arg.Any<Domain.Entities.User>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowBadRequest_WhenNewUserDoesNotAgreeToTerms()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, false);
        var userInfo = new OAuthUserInfo("google-sub-new", "new@example.com", null);

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-new", Arg.Any<CancellationToken>())
            .Returns((OAuthAccount?)null);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowBadRequest_WhenNewUserAgreeToTermsIsNull()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, null);
        var userInfo = new OAuthUserInfo("google-sub-new", "new@example.com", null);

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-new", Arg.Any<CancellationToken>())
            .Returns((OAuthAccount?)null);

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotRequireTermsAgreement_WhenExistingUserLogsIn()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", null, null);
        var userInfo = new OAuthUserInfo("google-sub-123", "test@example.com", null);
        var user = Domain.Entities.User.CreateWithOAuth("tester", "test@example.com", OAuthProvider.Google, "google-sub-123");
        var oAuthAccount = OAuthAccount.Create(user.Id, OAuthProvider.Google, "google-sub-123");

        _oAuthUserInfoProvider.GetUserInfoAsync(OAuthProvider.Google, "google-token", Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(OAuthProvider.Google, "google-sub-123", Arg.Any<CancellationToken>())
            .Returns(oAuthAccount);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);
        _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email!, user.Nickname).Returns("access-token");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("refresh-token");

        var act = () => _sut.ExecuteAsync(request, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStoreAgreedTermsVersion_WhenNewUserRegisters()
    {
        var request = new OAuthLoginRequest(OAuthProvider.Google, "google-token", "nick", true);
        var userInfo = new OAuthUserInfo("google-sub-123", "new@example.com", null);

        SetupNewUserMocks(userInfo);

        await _sut.ExecuteAsync(request, CancellationToken.None);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<Domain.Entities.User>(u => u.AgreedTermsVersion == "1.0"),
            Arg.Any<CancellationToken>());
    }

    private void SetupNewUserMocks(OAuthUserInfo userInfo)
    {
        _oAuthUserInfoProvider.GetUserInfoAsync(Arg.Any<OAuthProvider>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(userInfo);
        _oAuthAccountRepository.GetByProviderAndSubjectAsync(Arg.Any<OAuthProvider>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((OAuthAccount?)null);
        if (userInfo.Email is not null)
        {
            _userRepository.GetByEmailAsync(userInfo.Email, Arg.Any<CancellationToken>())
                .Returns((Domain.Entities.User?)null);
        }

        _jwtTokenGenerator.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns("at");
        _jwtTokenGenerator.GenerateRefreshToken().Returns("rt");
    }
}
