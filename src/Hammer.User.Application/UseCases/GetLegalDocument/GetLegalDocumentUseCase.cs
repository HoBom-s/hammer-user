using Hammer.User.Application.Common;
using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;

namespace Hammer.User.Application.UseCases.GetLegalDocument;

/// <summary>
///     Retrieves the latest legal document by type from the database.
/// </summary>
internal sealed class GetLegalDocumentUseCase(ILegalDocumentRepository repository) : IGetLegalDocumentUseCase
{
    public async Task<LegalDocumentResponse> ExecuteAsync(LegalDocumentType type, CancellationToken ct)
    {
        var document = await repository.GetLatestByTypeAsync(type, ct)
            ?? throw new NotFoundException($"법률 문서를 찾을 수 없어요: {type}");

        return new LegalDocumentResponse(
            document.Version,
            document.EffectiveDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            document.Content);
    }
}
