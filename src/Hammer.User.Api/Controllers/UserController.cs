using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.UserInfo;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     User information endpoints.
/// </summary>
[ApiController]
[Route("hammer-users/users")]
[Tags("User")]
public sealed class UserController(IGetUserInfoByIdUseCase getUserInfoByIdUseCase) : ControllerBase
{
    /// <summary>
    ///     유저 ID로 유저 정보를 조회한다.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the user information.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var response = await getUserInfoByIdUseCase.ExecuteAsync(id, ct);
        return Ok(response);
    }
}
