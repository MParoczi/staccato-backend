using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class SystemStylePresetConfiguration : IEntityTypeConfiguration<SystemStylePresetEntity>
{
    public void Configure(EntityTypeBuilder<SystemStylePresetEntity> builder)
    {
        builder.ToTable("SystemStylePresets");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.DisplayOrder).IsRequired();
        builder.Property(p => p.IsDefault).IsRequired();
        builder.Property(p => p.StylesJson).IsRequired().HasColumnType("nvarchar(max)");
    }
}
