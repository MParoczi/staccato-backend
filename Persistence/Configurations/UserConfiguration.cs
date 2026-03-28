using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.AvatarUrl).HasColumnType("nvarchar(max)");
        builder.Property(u => u.GoogleId).HasMaxLength(255);
        builder.Property(u => u.PasswordHash).HasColumnType("nvarchar(max)");

        builder.Property(u => u.DefaultPageSize)
            .HasConversion<string>()
            .HasColumnType("nvarchar(50)");

        builder.HasOne(u => u.DefaultInstrument)
            .WithMany()
            .HasForeignKey(u => u.DefaultInstrumentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");
    }
}