using Hammer.User.Application.UseCases.ValidateToken;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     Internal auth endpoints for gateway-side token validation.
/// </summary>
[ApiController]
[Route("hammer-users/internal/auth")]
[Tags("Internal")]
public sealed class InternalAuthController(IValidateTokenUseCase validateTokenUseCase) : ControllerBase
{
    /// <summary>
    ///     Access Token의 서명과 유효기간을 검증하고, 유저 상태를 확인하여 클레임을 반환한다.
    /// </summary>
    /// <param name="request">The token validation request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with user claims if valid; 401 if invalid.</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidateTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateAsync(ValidateTokenRequest request, CancellationToken ct)
    {
        var response = await validateTokenUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }
}
