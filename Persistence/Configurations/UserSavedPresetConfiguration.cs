using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class UserSavedPresetConfiguration : IEntityTypeConfiguration<UserSavedPresetEntity>
{
    public void Configure(EntityTypeBuilder<UserSavedPresetEntity> builder)
    {
        builder.ToTable("UserSavedPresets");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.StylesJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(p => p.User)
            .WithMany(u => u.UserSavedPresets)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}