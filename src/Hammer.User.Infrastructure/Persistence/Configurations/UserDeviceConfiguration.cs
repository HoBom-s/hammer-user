using Hammer.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.User.Infrastructure.Persistence.Configurations;

internal sealed class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("user_devices");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();

        builder.Property(d => d.UserId).IsRequired();
        builder.Property(d => d.Platform).IsRequired().HasConversion<short>();
        builder.Property(d => d.DeviceIdentifier).IsRequired().HasMaxLength(512);
        builder.Property(d => d.PushToken).IsRequired().HasMaxLength(512).HasColumnName("fcm_token");

        builder.HasIndex(d => d.UserId).IsUnique();
    }
}
