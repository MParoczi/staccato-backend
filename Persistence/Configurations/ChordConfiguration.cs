using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ChordConfiguration : IEntityTypeConfiguration<ChordEntity>
{
    public void Configure(EntityTypeBuilder<ChordEntity> builder)
    {
        builder.ToTable("Chords");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Root).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Quality).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Extension).HasMaxLength(50);
        builder.Property(c => c.Alternation).HasMaxLength(50);
        builder.Property(c => c.PositionsJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(c => c.Instrument)
            .WithMany(i => i.Chords)
            .HasForeignKey(c => c.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.InstrumentId, c.Root, c.Quality })
            .HasDatabaseName("IX_Chords_InstrumentId_Root_Quality");
    }
}