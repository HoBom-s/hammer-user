using Hammer.User.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.User.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<Domain.Entities.User>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property<uint>("xmin").IsRowVersion();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.Nickname).IsRequired().HasMaxLength(50);
        builder.Property(u => u.PasswordHash).HasMaxLength(256);
        builder.Property(u => u.Status).IsRequired().HasConversion<short>();
        builder.Property(u => u.DeletedAt);
        builder.Property(u => u.AgreedTermsVersion).HasMaxLength(20);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL");

        builder.HasQueryFilter(u => u.DeletedAt == null);

        builder.Ignore(u => u.IsDeleted);

        builder.HasOne(u => u.Device)
            .WithOne()
            .HasForeignKey<Domain.Entities.UserDevice>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.OAuthAccounts)
            .WithOne()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.OAuthAccounts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(u => u.RefreshTokens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
