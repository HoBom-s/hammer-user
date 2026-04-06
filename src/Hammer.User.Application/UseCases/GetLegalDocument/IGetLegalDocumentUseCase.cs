using Hammer.User.Application.Common;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.UseCases.GetLegalDocument;

/// <summary>
///     Retrieves the latest legal document by type.
/// </summary>
public interface IGetLegalDocumentUseCase
{
    public Task<LegalDocumentResponse> ExecuteAsync(LegalDocumentType type, CancellationToken ct);
}
