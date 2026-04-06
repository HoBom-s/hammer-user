using Hammer.User.Application.Exceptions;
using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;
using Hammer.User.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Hammer.User.Infrastructure.Persistence.Repositories;

internal sealed class LegalDocumentRepository(HammerUserDbContext context) : ILegalDocumentRepository
{
    public async Task<LegalDocument?> GetLatestByTypeAsync(LegalDocumentType type, CancellationToken cancellationToken = default) =>
        await context.LegalDocuments
            .Where(d => d.Type == type)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<LegalDocument?> GetByTypeAndVersionAsync(LegalDocumentType type, string version, CancellationToken cancellationToken = default) =>
        await context.LegalDocuments
            .FirstOrDefaultAsync(d => d.Type == type && d.Version == version, cancellationToken);

    public async Task AddAsync(LegalDocument document, CancellationToken cancellationToken = default) =>
        await context.LegalDocuments.AddAsync(document, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("이미 존재하는 버전입니다.");
        }
    }
}
