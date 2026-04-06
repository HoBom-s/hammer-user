using Hammer.User.Application.Common;

namespace Hammer.User.Application.UseCases.CreateLegalDocument;

/// <summary>
///     Creates a new versioned legal document.
/// </summary>
public interface ICreateLegalDocumentUseCase
{
    public Task<LegalDocumentResponse> ExecuteAsync(CreateLegalDocumentRequest request, CancellationToken ct);
}
