using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class NotebookModuleStyleConfiguration : IEntityTypeConfiguration<NotebookModuleStyleEntity>
{
    public void Configure(EntityTypeBuilder<NotebookModuleStyleEntity> builder)
    {
        builder.ToTable("NotebookModuleStyles");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ModuleType).IsRequired();
        builder.Property(s => s.StylesJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(s => s.Notebook)
            .WithMany(n => n.ModuleStyles)
            .HasForeignKey(s => s.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.NotebookId, s.ModuleType }).IsUnique();
    }
}