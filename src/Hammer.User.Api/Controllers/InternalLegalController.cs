using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.CreateLegalDocument;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     Internal legal document management endpoints.
/// </summary>
[ApiController]
[Route("hammer-users/internal/legal")]
[Tags("Internal")]
public sealed class InternalLegalController(ICreateLegalDocumentUseCase createLegalDocumentUseCase) : ControllerBase
{
    /// <summary>
    ///     새 버전의 법률 문서를 생성한다.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>201 Created with the new document.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(LegalDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync(CreateLegalDocumentRequest request, CancellationToken ct)
    {
        var response = await createLegalDocumentUseCase.ExecuteAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
