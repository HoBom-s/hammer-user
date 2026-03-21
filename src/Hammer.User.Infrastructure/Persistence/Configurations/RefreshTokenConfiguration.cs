using Hammer.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.User.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.Token).IsRequired().HasMaxLength(512);
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.RevokedAt);

        builder.HasIndex(r => r.Token).IsUnique();

        builder.Ignore(r => r.IsRevoked);
        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsActive);
    }
}
