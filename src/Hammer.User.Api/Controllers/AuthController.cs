using Hammer.User.Application.UseCases.Login;
using Hammer.User.Application.UseCases.Register;
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
    IRegisterUserUseCase registerUserUseCase) : ControllerBase
{
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

        Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = response.RefreshTokenExpiresAt,
            Path = "/hammer-users/auth",
        });

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
}
