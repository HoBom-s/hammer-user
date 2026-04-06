using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Ports;

/// <summary>
///     Repository for legal documents.
/// </summary>
public interface ILegalDocumentRepository
{
    public Task<Entities.LegalDocument?> GetLatestByTypeAsync(LegalDocumentType type, CancellationToken cancellationToken = default);

    public Task<Entities.LegalDocument?> GetByTypeAndVersionAsync(LegalDocumentType type, string version, CancellationToken cancellationToken = default);

    public Task AddAsync(Entities.LegalDocument document, CancellationToken cancellationToken = default);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
