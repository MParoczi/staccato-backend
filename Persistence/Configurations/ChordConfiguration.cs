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
        builder.Property(c => c.Suffix).IsRequired().HasMaxLength(200);
        builder.Property(c => c.PositionsJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(c => c.Instrument)
            .WithMany(i => i.Chords)
            .HasForeignKey(c => c.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}