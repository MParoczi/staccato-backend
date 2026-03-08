using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class LessonPageConfiguration : IEntityTypeConfiguration<LessonPageEntity>
{
    public void Configure(EntityTypeBuilder<LessonPageEntity> builder)
    {
        builder.ToTable("LessonPages");

        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.PageNumber).IsRequired();

        builder.HasOne(lp => lp.Lesson)
            .WithMany(l => l.LessonPages)
            .HasForeignKey(lp => lp.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}