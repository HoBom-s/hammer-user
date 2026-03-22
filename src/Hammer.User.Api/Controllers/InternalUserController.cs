using Hammer.User.Application.UseCases.GetUsers;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     Internal user management endpoints.
/// </summary>
[ApiController]
[Route("hammer-users/internal/users")]
[Tags("Internal")]
public sealed class InternalUserController(IGetUsersUseCase getUsersUseCase) : ControllerBase
{
    /// <summary>
    ///     유저 목록을 페이징하여 조회한다. Status 필터를 선택적으로 적용할 수 있다.
    /// </summary>
    /// <param name="request">The paged query parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the paged user list.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsersAsync(
        [FromQuery] GetUsersRequest request, CancellationToken ct)
    {
        var response = await getUsersUseCase.ExecuteAsync(request, ct);
        return Ok(response);
    }
}
