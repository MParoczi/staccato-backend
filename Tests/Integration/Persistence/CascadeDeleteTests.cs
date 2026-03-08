using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Tests.Integration.Persistence;

/// <summary>
///     SC-006: Deleting a User cascades to all 7 dependent entity types without errors.
///     Uses InMemory EF — cascade behaviour is handled by EF Core (not the DB engine).
/// </summary>
public class CascadeDeleteTests
{
    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    [Fact]
    public async Task DeleteUser_CascadesToAllSevenDependentEntityTypes()
    {
        await using var ctx = CreateContext();

        // ── seed immutable reference data ────────────────────────────────────
        var instrId = Guid.NewGuid();
        ctx.Instruments.Add(new InstrumentEntity
        {
            Id = instrId, Key = InstrumentKey.Guitar6String,
            DisplayName = "Guitar", StringCount = 6
        });
        await ctx.SaveChangesAsync();

        // ── build user with one of each dependent type ───────────────────────
        var userId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var pageId = Guid.NewGuid();

        var user = new UserEntity
        {
            Id = userId, Email = "cascade@test.com",
            FirstName = "Cascade", LastName = "Test",
            CreatedAt = DateTime.UtcNow
        };
        ctx.Users.Add(user);

        ctx.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(), Token = "tok", UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow
        });
        ctx.UserSavedPresets.Add(new UserSavedPresetEntity
        {
            Id = Guid.NewGuid(), UserId = userId,
            Name = "Preset", StylesJson = "[]"
        });

        var notebook = new NotebookEntity
        {
            Id = notebookId, UserId = userId, Title = "NB",
            InstrumentId = instrId, PageSize = PageSize.A4,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Notebooks.Add(notebook);

        ctx.NotebookModuleStyles.Add(new NotebookModuleStyleEntity
        {
            Id = Guid.NewGuid(), NotebookId = notebookId,
            ModuleType = ModuleType.Theory, StylesJson = "{}"
        });

        ctx.PdfExports.Add(new PdfExportEntity
        {
            Id = Guid.NewGuid(), NotebookId = notebookId, UserId = userId,
            Status = ExportStatus.Pending, CreatedAt = DateTime.UtcNow
        });

        var lesson = new LessonEntity
        {
            Id = lessonId, NotebookId = notebookId,
            Title = "Lesson", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        ctx.Lessons.Add(lesson);

        var page = new LessonPageEntity
            { Id = pageId, LessonId = lessonId, PageNumber = 1 };
        ctx.LessonPages.Add(page);

        ctx.Modules.Add(new ModuleEntity
        {
            Id = Guid.NewGuid(), LessonPageId = pageId,
            ModuleType = ModuleType.Theory,
            GridX = 0, GridY = 0, GridWidth = 8, GridHeight = 5,
            ContentJson = "[]"
        });

        await ctx.SaveChangesAsync();

        // ── verify rows exist before deletion ────────────────────────────────
        Assert.Equal(1, await ctx.RefreshTokens.CountAsync());
        Assert.Equal(1, await ctx.UserSavedPresets.CountAsync());
        Assert.Equal(1, await ctx.Notebooks.CountAsync());
        Assert.Equal(1, await ctx.NotebookModuleStyles.CountAsync());
        Assert.Equal(1, await ctx.PdfExports.CountAsync());
        Assert.Equal(1, await ctx.Lessons.CountAsync());
        Assert.Equal(1, await ctx.LessonPages.CountAsync());
        Assert.Equal(1, await ctx.Modules.CountAsync());

        // ── load navigation properties required for EF ClientCascade ─────────
        // PdfExports.UserId uses ClientCascade — EF needs to track PdfExports
        // before removing the User so it can delete them in-memory.
        await ctx.PdfExports.LoadAsync();

        // ── delete the user ───────────────────────────────────────────────────
        ctx.Users.Remove(user);
        await ctx.SaveChangesAsync(); // should not throw

        // ── verify all dependents are gone ────────────────────────────────────
        Assert.Equal(0, await ctx.Users.CountAsync());
        Assert.Equal(0, await ctx.RefreshTokens.CountAsync());
        Assert.Equal(0, await ctx.UserSavedPresets.CountAsync());
        Assert.Equal(0, await ctx.Notebooks.CountAsync());
        Assert.Equal(0, await ctx.NotebookModuleStyles.CountAsync());
        Assert.Equal(0, await ctx.PdfExports.CountAsync());
        Assert.Equal(0, await ctx.Lessons.CountAsync());
        Assert.Equal(0, await ctx.LessonPages.CountAsync());
        Assert.Equal(0, await ctx.Modules.CountAsync());

        // ── instruments and chords are untouched ─────────────────────────────
        Assert.Equal(1, await ctx.Instruments.CountAsync());
    }
}