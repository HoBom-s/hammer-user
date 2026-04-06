using System.Security.Cryptography;
using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Application.UseCases.Login;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.Extensions.Options;

namespace Hammer.User.Application.UseCases.OAuthLogin;

/// <summary>
///     Authenticates a user via OAuth provider, creating a new account if needed.
/// </summary>
internal sealed class OAuthLoginUseCase(
    IOAuthUserInfoProvider oAuthUserInfoProvider,
    IOAuthAccountRepository oAuthAccountRepository,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    IOptions<JwtSettings> jwtSettings,
    ILegalDocumentRepository legalDocumentRepository)
    : IOAuthLoginUseCase
{
    public async Task<LoginUserResponse> ExecuteAsync(OAuthLoginRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userInfo = await oAuthUserInfoProvider.GetUserInfoAsync(request.Provider, request.Token, ct);

        var existingAccount = await oAuthAccountRepository.GetByProviderAndSubjectAsync(
            request.Provider,
            userInfo.ProviderSubjectId,
            ct);

        if (existingAccount is not null)
            return await LoginExistingUserAsync(existingAccount.UserId, ct);

        return await RegisterAndLoginAsync(request, userInfo, ct);
    }

    private static string ResolveNickname(string? requestNickname, OAuthUserInfo userInfo)
    {
        if (!string.IsNullOrWhiteSpace(requestNickname))
            return requestNickname;

        if (!string.IsNullOrWhiteSpace(userInfo.Nickname))
            return userInfo.Nickname;

        if (userInfo.Email is not null)
        {
            var localPart = userInfo.Email.Split('@')[0];

            if (!string.IsNullOrWhiteSpace(localPart))
                return localPart;
        }

        var hex = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        return $"user_{hex.ToUpperInvariant()}";
    }

    private async Task<LoginUserResponse> LoginExistingUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("사용자를 찾을 수 없습니다.");

        if (!user.IsActive)
            throw new UnauthorizedException("비활성화된 계정입니다.");

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email ?? string.Empty, user.Nickname);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays);

        user.IssueRefreshToken(refreshToken, refreshTokenExpiresAt);
        await userRepository.SaveChangesAsync(ct);

        return new LoginUserResponse(accessToken, refreshToken, refreshTokenExpiresAt);
    }

    private async Task<LoginUserResponse> RegisterAndLoginAsync(
        OAuthLoginRequest request,
        OAuthUserInfo userInfo,
        CancellationToken ct)
    {
        if (request.AgreeToTerms != true)
            throw new BadRequestException("이용약관에 동의해야 합니다.");

        if (userInfo.Email is not null)
        {
            var existingUser = await userRepository.GetByEmailAsync(userInfo.Email, ct);

            if (existingUser is not null)
                throw new ConflictException("이미 해당 이메일로 가입된 계정이 존재합니다. 기존 방식으로 로그인해 주세요.");
        }

        var terms = await legalDocumentRepository.GetLatestByTypeAsync(LegalDocumentType.TermsOfService, ct)
            ?? throw new NotFoundException("이용약관을 찾을 수 없습니다.");

        var nickname = ResolveNickname(request.Nickname, userInfo);

        var user = Domain.Entities.User.CreateWithOAuth(
            nickname,
            userInfo.Email,
            request.Provider,
            userInfo.ProviderSubjectId,
            terms.Version);

        await userRepository.AddAsync(user, ct);

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email ?? string.Empty, user.Nickname);
        var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtSettings.Value.RefreshTokenExpiryDays);

        user.IssueRefreshToken(refreshToken, refreshTokenExpiresAt);
        await userRepository.SaveChangesAsync(ct);

        return new LoginUserResponse(accessToken, refreshToken, refreshTokenExpiresAt);
    }
}
