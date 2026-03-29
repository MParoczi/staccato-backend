using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<ModuleEntity>
{
    public void Configure(EntityTypeBuilder<ModuleEntity> builder)
    {
        builder.ToTable("Modules");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModuleType).IsRequired();
        builder.Property(m => m.GridX).IsRequired();
        builder.Property(m => m.GridY).IsRequired();
        builder.Property(m => m.GridWidth).IsRequired();
        builder.Property(m => m.GridHeight).IsRequired();
        builder.Property(m => m.ZIndex).IsRequired();
        builder.Property(m => m.ContentJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(m => m.LessonPage)
            .WithMany(lp => lp.Modules)
            .HasForeignKey(m => m.LessonPageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}