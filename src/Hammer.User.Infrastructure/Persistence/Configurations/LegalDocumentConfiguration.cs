using Hammer.User.Domain.Entities;
using Hammer.User.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.User.Infrastructure.Persistence.Configurations;

internal sealed class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> builder)
    {
        builder.ToTable("legal_documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();

        builder.Property(d => d.Type).IsRequired().HasConversion<short>();
        builder.Property(d => d.Version).IsRequired().HasMaxLength(20);
        builder.Property(d => d.EffectiveDate).IsRequired();
        builder.Property(d => d.Content).IsRequired();

        builder.HasIndex(d => new { d.Type, d.Version }).IsUnique();
    }
}
