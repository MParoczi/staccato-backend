using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class InstrumentConfiguration : IEntityTypeConfiguration<InstrumentEntity>
{
    public void Configure(EntityTypeBuilder<InstrumentEntity> builder)
    {
        builder.ToTable("Instruments");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Key).IsRequired();
        builder.Property(i => i.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.StringCount).IsRequired();

        builder.HasIndex(i => i.Key).IsUnique();
    }
}