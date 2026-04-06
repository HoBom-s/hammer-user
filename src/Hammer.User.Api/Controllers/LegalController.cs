using Hammer.User.Application.Common;
using Hammer.User.Application.UseCases.GetLegalDocument;
using Hammer.User.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Hammer.User.Api.Controllers;

/// <summary>
///     Legal document endpoints (terms of service, privacy policy).
/// </summary>
[ApiController]
[Route("hammer-users/legal")]
[Tags("Legal")]
public sealed class LegalController(IGetLegalDocumentUseCase getLegalDocumentUseCase) : ControllerBase
{
    /// <summary>
    ///     이용약관을 조회한다. 데이터 출처 정보를 포함한다.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the terms of service document.</returns>
    [HttpGet("terms")]
    [ProducesResponseType(typeof(LegalDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTermsOfServiceAsync(CancellationToken ct)
    {
        var response = await getLegalDocumentUseCase.ExecuteAsync(LegalDocumentType.TermsOfService, ct);
        return Ok(response);
    }

    /// <summary>
    ///     개인정보처리방침을 조회한다.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>200 OK with the privacy policy document.</returns>
    [HttpGet("privacy")]
    [ProducesResponseType(typeof(LegalDocumentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrivacyPolicyAsync(CancellationToken ct)
    {
        var response = await getLegalDocumentUseCase.ExecuteAsync(LegalDocumentType.PrivacyPolicy, ct);
        return Ok(response);
    }
}
