using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<UserSavedPresetEntity> UserSavedPresets => Set<UserSavedPresetEntity>();
    public DbSet<SystemStylePresetEntity> SystemStylePresets => Set<SystemStylePresetEntity>();
    public DbSet<InstrumentEntity> Instruments => Set<InstrumentEntity>();
    public DbSet<ChordEntity> Chords => Set<ChordEntity>();
    public DbSet<NotebookEntity> Notebooks => Set<NotebookEntity>();
    public DbSet<NotebookModuleStyleEntity> NotebookModuleStyles => Set<NotebookModuleStyleEntity>();
    public DbSet<LessonEntity> Lessons => Set<LessonEntity>();
    public DbSet<LessonPageEntity> LessonPages => Set<LessonPageEntity>();
    public DbSet<ModuleEntity> Modules => Set<ModuleEntity>();
    public DbSet<PdfExportEntity> PdfExports => Set<PdfExportEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}