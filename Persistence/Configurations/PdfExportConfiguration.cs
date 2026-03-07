using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class PdfExportConfiguration : IEntityTypeConfiguration<PdfExportEntity>
{
    public void Configure(EntityTypeBuilder<PdfExportEntity> builder)
    {
        builder.ToTable("PdfExports");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.BlobReference).HasColumnType("nvarchar(max)");
        builder.Property(e => e.LessonIdsJson).HasColumnType("nvarchar(max)");

        builder.HasOne(e => e.Notebook)
            .WithMany(n => n.PdfExports)
            .HasForeignKey(e => e.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        // ClientCascade: ON DELETE NO ACTION in SQL Server (avoids multiple cascade paths
        // from Users → PdfExports). EF Core deletes related PdfExports in memory before
        // deleting the UserEntity. See FR-042.
        builder.HasOne(e => e.User)
            .WithMany(u => u.PdfExports)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.ClientCascade);

        // Partial unique index: at most one active export (Pending=0 or Processing=1) per notebook.
        builder.HasIndex(e => e.NotebookId)
            .IsUnique()
            .HasFilter("[Status] = 0 OR [Status] = 1");
    }
}
