using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.GetDeviceToken;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     Internal user management endpoints.
/// </summary>
[ApiController]
[Route("hammer-users/internal/users")]
[Tags("Internal")]
public sealed class InternalUserController(
    IGetDeviceTokenUseCase getDeviceTokenUseCase) : ControllerBase
{
    /// <summary>
    ///     사용자의 디바이스 푸시 토큰을 조회한다.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with push token, or 404 if no device registered.</returns>
    [HttpGet("{userId:guid}/device-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDeviceTokenAsync(Guid userId, CancellationToken ct)
    {
        var token = await getDeviceTokenUseCase.ExecuteAsync(userId, ct);

        if (token is null)
            return NotFound();

        return Ok(new DeviceTokenResponse(token));
    }
}
