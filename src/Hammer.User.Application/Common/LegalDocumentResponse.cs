namespace Hammer.User.Application.Common;

/// <summary>
///     Response DTO for legal documents (terms of service, privacy policy).
/// </summary>
/// <param name="Version">The document version.</param>
/// <param name="EffectiveDate">The date the document became effective.</param>
/// <param name="Content">The document content in Markdown format.</param>
public sealed record LegalDocumentResponse(string Version, string EffectiveDate, string Content);
