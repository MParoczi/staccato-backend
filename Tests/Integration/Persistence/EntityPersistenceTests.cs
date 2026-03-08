using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Tests.Integration.Persistence;

/// <summary>
///     Round-trip persistence tests for all 12 entity types.
///     Each test inserts one row, detaches it, queries it back, and verifies all scalar properties.
/// </summary>
public class EntityPersistenceTests
{
    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    // ── UserEntity ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UserEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        ctx.Users.Add(new UserEntity
        {
            Id = id, Email = "test@example.com", PasswordHash = "hash",
            GoogleId = "gid123", FirstName = "Alice", LastName = "Smith",
            AvatarUrl = "https://example.com/avatar.png",
            CreatedAt = now, ScheduledDeletionAt = now.AddDays(30),
            Language = Language.English
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.Users.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal("test@example.com", entity.Email);
        Assert.Equal("hash", entity.PasswordHash);
        Assert.Equal("gid123", entity.GoogleId);
        Assert.Equal("Alice", entity.FirstName);
        Assert.Equal("Smith", entity.LastName);
        Assert.Equal("https://example.com/avatar.png", entity.AvatarUrl);
        Assert.Equal(now, entity.CreatedAt);
        Assert.Equal(Language.English, entity.Language);
        Assert.NotNull(entity.ScheduledDeletionAt);
    }

    // ── RefreshTokenEntity ────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var userId = Guid.NewGuid();
        ctx.Users.Add(new UserEntity { Id = userId, Email = "u@u.com", FirstName = "U", LastName = "U", CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        var exp = DateTime.UtcNow.AddDays(7);
        ctx.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = id, Token = "tok-abc", UserId = userId,
            ExpiresAt = exp, CreatedAt = DateTime.UtcNow, IsRevoked = false
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.RefreshTokens.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal("tok-abc", entity.Token);
        Assert.Equal(userId, entity.UserId);
        Assert.Equal(exp, entity.ExpiresAt);
        Assert.False(entity.IsRevoked);
    }

    // ── UserSavedPresetEntity ─────────────────────────────────────────────────

    [Fact]
    public async Task UserSavedPresetEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var userId = Guid.NewGuid();
        ctx.Users.Add(new UserEntity { Id = userId, Email = "u@u.com", FirstName = "U", LastName = "U", CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        const string json = "[{\"moduleType\":\"Title\"}]";
        ctx.UserSavedPresets.Add(new UserSavedPresetEntity
        {
            Id = id, UserId = userId, Name = "My Preset", StylesJson = json
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.UserSavedPresets.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal("My Preset", entity.Name);
        Assert.Equal(json, entity.StylesJson);
        Assert.Equal(userId, entity.UserId);
    }

    // ── SystemStylePresetEntity ───────────────────────────────────────────────

    [Fact]
    public async Task SystemStylePresetEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var id = Guid.NewGuid();
        const string json = "[{\"moduleType\":\"Title\"}]";

        ctx.SystemStylePresets.Add(new SystemStylePresetEntity
        {
            Id = id, Name = "Classic", DisplayOrder = 1, IsDefault = true, StylesJson = json
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.SystemStylePresets.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal("Classic", entity.Name);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsDefault);
        Assert.Equal(json, entity.StylesJson);
    }

    // ── InstrumentEntity ──────────────────────────────────────────────────────

    [Fact]
    public async Task InstrumentEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var id = Guid.NewGuid();

        ctx.Instruments.Add(new InstrumentEntity
        {
            Id = id, Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar", StringCount = 6
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.Instruments.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal(InstrumentKey.Guitar6String, entity.Key);
        Assert.Equal("6-String Guitar", entity.DisplayName);
        Assert.Equal(6, entity.StringCount);
    }

    // ── ChordEntity ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ChordEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var instrId = Guid.NewGuid();
        ctx.Instruments.Add(new InstrumentEntity
        {
            Id = instrId, Key = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar", StringCount = 6
        });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        const string posJson = "[{\"label\":\"1\",\"baseFret\":1}]";
        ctx.Chords.Add(new ChordEntity
        {
            Id = id, InstrumentId = instrId,
            Name = "A", Suffix = "major", PositionsJson = posJson
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.Chords.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal("A", entity.Name);
        Assert.Equal("major", entity.Suffix);
        Assert.Equal(instrId, entity.InstrumentId);
        Assert.Equal(posJson, entity.PositionsJson);
    }

    // ── NotebookEntity ────────────────────────────────────────────────────────

    [Fact]
    public async Task NotebookEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var userId = Guid.NewGuid();
        var instrId = Guid.NewGuid();
        ctx.Users.Add(new UserEntity { Id = userId, Email = "u@u.com", FirstName = "U", LastName = "U", CreatedAt = DateTime.UtcNow });
        ctx.Instruments.Add(new InstrumentEntity { Id = instrId, Key = InstrumentKey.Guitar6String, DisplayName = "Guitar", StringCount = 6 });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        ctx.Notebooks.Add(new NotebookEntity
        {
            Id = id, UserId = userId, Title = "My Notebook",
            InstrumentId = instrId, PageSize = PageSize.A4,
            CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.Notebooks.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal("My Notebook", entity.Title);
        Assert.Equal(PageSize.A4, entity.PageSize);
        Assert.Equal(userId, entity.UserId);
        Assert.Equal(instrId, entity.InstrumentId);
    }

    // ── NotebookModuleStyleEntity ─────────────────────────────────────────────

    [Fact]
    public async Task NotebookModuleStyleEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var userId = Guid.NewGuid();
        var instrId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        ctx.Users.Add(new UserEntity { Id = userId, Email = "u@u.com", FirstName = "U", LastName = "U", CreatedAt = DateTime.UtcNow });
        ctx.Instruments.Add(new InstrumentEntity { Id = instrId, Key = InstrumentKey.Guitar6String, DisplayName = "Guitar", StringCount = 6 });
        ctx.Notebooks.Add(new NotebookEntity
        {
            Id = notebookId, UserId = userId, Title = "NB", InstrumentId = instrId, PageSize = PageSize.A4, CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        const string styleJson = "{\"backgroundColor\":\"#FFF\"}";
        ctx.NotebookModuleStyles.Add(new NotebookModuleStyleEntity
        {
            Id = id, NotebookId = notebookId,
            ModuleType = ModuleType.Theory, StylesJson = styleJson
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.NotebookModuleStyles.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal(notebookId, entity.NotebookId);
        Assert.Equal(ModuleType.Theory, entity.ModuleType);
        Assert.Equal(styleJson, entity.StylesJson);
    }

    // ── LessonEntity ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LessonEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var userId = Guid.NewGuid();
        var instrId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        ctx.Users.Add(new UserEntity { Id = userId, Email = "u@u.com", FirstName = "U", LastName = "U", CreatedAt = DateTime.UtcNow });
        ctx.Instruments.Add(new InstrumentEntity { Id = instrId, Key = InstrumentKey.Guitar6String, DisplayName = "Guitar", StringCount = 6 });
        ctx.Notebooks.Add(new NotebookEntity
        {
            Id = notebookId, UserId = userId, Title = "NB", InstrumentId = instrId, PageSize = PageSize.A4, CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        ctx.Lessons.Add(new LessonEntity
        {
            Id = id, NotebookId = notebookId,
            Title = "Lesson 1", CreatedAt = now, UpdatedAt = now
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.Lessons.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal("Lesson 1", entity.Title);
        Assert.Equal(notebookId, entity.NotebookId);
        Assert.Equal(now, entity.CreatedAt);
    }

    // ── LessonPageEntity ──────────────────────────────────────────────────────

    [Fact]
    public async Task LessonPageEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var (lessonId, _) = await SeedLessonAsync(ctx);

        var id = Guid.NewGuid();
        ctx.LessonPages.Add(new LessonPageEntity { Id = id, LessonId = lessonId, PageNumber = 1 });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.LessonPages.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal(lessonId, entity.LessonId);
        Assert.Equal(1, entity.PageNumber);
    }

    // ── ModuleEntity ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ModuleEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var (lessonId, _) = await SeedLessonAsync(ctx);
        var pageId = Guid.NewGuid();
        ctx.LessonPages.Add(new LessonPageEntity { Id = pageId, LessonId = lessonId, PageNumber = 1 });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        const string contentJson = "[{\"type\":\"Text\"}]";
        ctx.Modules.Add(new ModuleEntity
        {
            Id = id, LessonPageId = pageId, ModuleType = ModuleType.Theory,
            GridX = 0, GridY = 0, GridWidth = 10, GridHeight = 5,
            ContentJson = contentJson
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.Modules.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal(ModuleType.Theory, entity.ModuleType);
        Assert.Equal(0, entity.GridX);
        Assert.Equal(0, entity.GridY);
        Assert.Equal(10, entity.GridWidth);
        Assert.Equal(5, entity.GridHeight);
        Assert.Equal(contentJson, entity.ContentJson);
    }

    // ── PdfExportEntity ───────────────────────────────────────────────────────

    [Fact]
    public async Task PdfExportEntity_PersistsAndRoundTrips()
    {
        await using var ctx = CreateContext();
        var userId = Guid.NewGuid();
        var instrId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        ctx.Users.Add(new UserEntity { Id = userId, Email = "u@u.com", FirstName = "U", LastName = "U", CreatedAt = DateTime.UtcNow });
        ctx.Instruments.Add(new InstrumentEntity { Id = instrId, Key = InstrumentKey.Guitar6String, DisplayName = "Guitar", StringCount = 6 });
        ctx.Notebooks.Add(new NotebookEntity
        {
            Id = notebookId, UserId = userId, Title = "NB", InstrumentId = instrId, PageSize = PageSize.A4, CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        const string lessonIdsJson = "[\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"]";
        ctx.PdfExports.Add(new PdfExportEntity
        {
            Id = id, NotebookId = notebookId, UserId = userId,
            Status = ExportStatus.Pending, CreatedAt = now,
            CompletedAt = null, BlobReference = null,
            LessonIdsJson = lessonIdsJson
        });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var entity = await ctx.PdfExports.FindAsync(id);
        Assert.NotNull(entity);
        Assert.Equal(ExportStatus.Pending, entity.Status);
        Assert.Equal(notebookId, entity.NotebookId);
        Assert.Equal(userId, entity.UserId);
        Assert.Equal(lessonIdsJson, entity.LessonIdsJson);
        Assert.Null(entity.CompletedAt);
        Assert.Null(entity.BlobReference);
    }

    // ── shared setup helpers ──────────────────────────────────────────────────

    private static async Task<(Guid LessonId, Guid NotebookId)> SeedLessonAsync(AppDbContext ctx)
    {
        var userId = Guid.NewGuid();
        var instrId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        ctx.Users.Add(new UserEntity { Id = userId, Email = "u@u.com", FirstName = "U", LastName = "U", CreatedAt = DateTime.UtcNow });
        ctx.Instruments.Add(new InstrumentEntity { Id = instrId, Key = InstrumentKey.Guitar6String, DisplayName = "Guitar", StringCount = 6 });
        ctx.Notebooks.Add(new NotebookEntity
        {
            Id = notebookId, UserId = userId, Title = "NB", InstrumentId = instrId, PageSize = PageSize.A4, CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        ctx.Lessons.Add(new LessonEntity { Id = lessonId, NotebookId = notebookId, Title = "L1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        return (lessonId, notebookId);
    }
}