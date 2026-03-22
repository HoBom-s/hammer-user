using Hammer.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.User.Infrastructure.Persistence.Configurations;

internal sealed class OAuthAccountConfiguration : IEntityTypeConfiguration<OAuthAccount>
{
    public void Configure(EntityTypeBuilder<OAuthAccount> builder)
    {
        builder.ToTable("oauth_accounts");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.Provider).IsRequired().HasConversion<short>();
        builder.Property(o => o.ProviderSubjectId).IsRequired().HasMaxLength(256);

        builder.HasIndex(o => new { o.Provider, o.ProviderSubjectId }).IsUnique();
    }
}
