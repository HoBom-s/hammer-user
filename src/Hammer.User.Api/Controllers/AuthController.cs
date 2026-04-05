using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.Device;
using Hammer.User.Application.UseCases.Login;
using Hammer.User.Application.UseCases.Logout;
using Hammer.User.Application.UseCases.OAuthLogin;
using Hammer.User.Application.UseCases.RefreshToken;
using Hammer.User.Application.UseCases.Register;
using Hammer.User.Application.UseCases.UserInfo;
using Hammer.User.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     Authentication endpoints.
/// </summary>
[ApiController]
[Route("hammer-users/auth")]
[Tags("Auth")]
public sealed class AuthController(
    ILoginUserUseCase loginUserUseCase,
    IOAuthLoginUseCase oAuthLoginUseCase,
    IRefreshTokenUseCase refreshTokenUseCase,
    IRegisterUserUseCase registerUserUseCase,
    IRegisterDeviceUseCase registerDeviceUseCase,
    ILogoutUseCase logoutUseCase,
    IGetUserInfoByTokenUseCase getUserInfoByTokenUseCase,
    IDeleteUserInfoByIdUseCase deleteUserInfoByIdUseCase,
    IJwtTokenGenerator jwtTokenGenerator) : ControllerBase
{
    private const string Path = "/hammer-users/auth";
    private const string RefreshToken = "refresh_token";
    private const string Authorization = "Authorization";
    private const string Bearer = "Bearer ";

    /// <summary>
    ///     이메일/비밀번호로 로그인하여 Access Token을 발급한다. Refresh Token은 HttpOnly Cookie로 설정된다.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the access token.</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync(LoginUserRequest request, CancellationToken ct)
    {
        var response = await loginUserUseCase.ExecuteAsync(request, ct);
        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);
        return Ok(new { response.AccessToken });
    }

    /// <summary>
    ///     OAuth 제공자 토큰으로 로그인하여 Access Token을 발급한다. 신규 유저는 자동으로 생성된다.
    /// </summary>
    /// <param name="request">The OAuth login request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the access token.</returns>
    [HttpPost("oauth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OAuthLoginAsync(OAuthLoginRequest request, CancellationToken ct)
    {
        var response = await oAuthLoginUseCase.ExecuteAsync(request, ct);
        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);
        return Ok(new { response.AccessToken });
    }

    /// <summary>
    ///     Refresh Token 쿠키를 사용하여 새로운 Access Token과 Refresh Token을 발급한다.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the new access token.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshToken, out var refreshToken))
            return Unauthorized();

        var response = await refreshTokenUseCase.ExecuteAsync(refreshToken, ct);
        SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);
        return Ok(new { response.AccessToken });
    }

    /// <summary>
    ///     이메일/비밀번호로 회원가입한다. 비밀번호는 영문, 숫자, 특수문자를 각각 하나 이상 포함해야 한다.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>201 Created with the registered user details.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterAsync(RegisterUserRequest request, CancellationToken ct)
    {
        var response = await registerUserUseCase.ExecuteAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    ///     디바이스를 등록(upsert)한다. Bearer 토큰에서 유저를 식별한다.
    /// </summary>
    /// <param name="authorization">The Authorization header value.</param>
    /// <param name="body">The device registration body.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpPut("device")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpsertDeviceAsync(
        [FromHeader(Name = Authorization)] string? authorization,
        RegisterDeviceBody body,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(body);

        var claims = ExtractBearerClaims(authorization);

        if (claims is null)
            return Unauthorized();

        var request = new RegisterDeviceRequest(claims.UserId, body.Platform, body.DeviceIdentifier, body.PushToken);
        await registerDeviceUseCase.ExecuteAsync(request, ct);
        return Ok();
    }

    /// <summary>
    ///     회원을 탈퇴한다.
    /// </summary>
    /// <param name="authorization">The Authorization header value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK on success.</returns>
    [HttpDelete("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserAsync(
        [FromHeader(Name = Authorization)] string? authorization, CancellationToken ct)
    {
        var claims = ExtractBearerClaims(authorization);

        if (claims is null)
            return Unauthorized();

        var response = await deleteUserInfoByIdUseCase.ExecuteAsync(claims.UserId, ct);

        Response.Cookies.Delete(
            RefreshToken,
            new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = Path,
            });

        return Ok(response);
    }

    /// <summary>
    ///     로그아웃한다. Refresh Token을 폐기하고 디바이스를 제거한다.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>204 NoContent on success.</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshToken, out var refreshToken))
            return Unauthorized();

        await logoutUseCase.ExecuteAsync(refreshToken, ct);

        Response.Cookies.Delete(
            RefreshToken,
            new CookieOptions
            {
                HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = Path,
            });

        return NoContent();
    }

    /// <summary>
    ///     인증된 유저의 상세 정보를 조회한다. 디바이스 정보를 포함한다.
    /// </summary>
    /// <param name="authorization">The Authorization header value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the user detail response.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserInfoDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMeAsync(
        [FromHeader(Name = Authorization)] string? authorization,
        CancellationToken ct)
    {
        var claims = ExtractBearerClaims(authorization);

        if (claims is null)
            return Unauthorized();

        var response = await getUserInfoByTokenUseCase.ExecuteAsync(claims.UserId, ct);
        return Ok(response);
    }

    private AccessTokenClaims? ExtractBearerClaims(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
            return null;

        if (!authorization.StartsWith(Bearer, StringComparison.OrdinalIgnoreCase))
            return null;

        return jwtTokenGenerator.ValidateAccessToken(authorization[Bearer.Length..]);
    }

    private void SetRefreshTokenCookie(string token, DateTimeOffset expiresAt)
    {
        Response.Cookies.Append(
            RefreshToken,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresAt,
                Path = Path,
            });
    }
}
