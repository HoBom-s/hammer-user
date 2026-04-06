using System.ComponentModel.DataAnnotations;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Application.UseCases.CreateLegalDocument;

/// <summary>
///     Request DTO for creating a legal document.
/// </summary>
public sealed record CreateLegalDocumentRequest(
    [Required] LegalDocumentType Type,
    [Required] string Version,
    [Required] DateTimeOffset EffectiveDate,
    [Required] string Content);
