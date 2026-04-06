using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.CreateLegalDocument;

/// <summary>
///     Creates a new versioned legal document. Throws on duplicate version.
/// </summary>
internal sealed class CreateLegalDocumentUseCase(ILegalDocumentRepository repository) : ICreateLegalDocumentUseCase
{
    public async Task<LegalDocumentResponse> ExecuteAsync(CreateLegalDocumentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await repository.GetByTypeAndVersionAsync(request.Type, request.Version, ct);

        if (existing is not null)
            throw new ConflictException($"이미 존재하는 버전입니다: {request.Type} v{request.Version}");

        var document = LegalDocument.Create(request.Type, request.Version, request.EffectiveDate, request.Content);
        await repository.AddAsync(document, ct);
        await repository.SaveChangesAsync(ct);

        return new LegalDocumentResponse(
            document.Version,
            document.EffectiveDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            document.Content);
    }
}
