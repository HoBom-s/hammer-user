using Hammer.User.Domain.Common;
using Hammer.User.Domain.Enums;

namespace Hammer.User.Domain.Entities;

/// <summary>
///     Legal document entity storing versioned terms and policies.
/// </summary>
public sealed class LegalDocument : Entity
{
    private LegalDocument()
    {
    }

    /// <summary>
    ///     Gets the document type.
    /// </summary>
    public LegalDocumentType Type { get; private init; }

    /// <summary>
    ///     Gets the document version (e.g. "1.0").
    /// </summary>
    public string Version { get; private init; } = string.Empty;

    /// <summary>
    ///     Gets the date when this document becomes effective.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; private init; }

    /// <summary>
    ///     Gets the document content in Markdown format.
    /// </summary>
    public string Content { get; private init; } = string.Empty;

    /// <summary>
    ///     Creates a new legal document.
    /// </summary>
    /// <param name="type">The document type.</param>
    /// <param name="version">The document version.</param>
    /// <param name="effectiveDate">The effective date.</param>
    /// <param name="content">The document content.</param>
    /// <returns>A new <see cref="LegalDocument" /> instance.</returns>
    public static LegalDocument Create(LegalDocumentType type, string version, DateTimeOffset effectiveDate, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new LegalDocument
        {
            Type = type,
            Version = version,
            EffectiveDate = effectiveDate,
            Content = content,
        };
    }
}
