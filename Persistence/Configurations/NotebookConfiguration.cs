using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class NotebookConfiguration : IEntityTypeConfiguration<NotebookEntity>
{
    public void Configure(EntityTypeBuilder<NotebookEntity> builder)
    {
        builder.ToTable("Notebooks");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(n => n.PageSize).IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.UpdatedAt).IsRequired();

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notebooks)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Instrument)
            .WithMany()
            .HasForeignKey(n => n.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}