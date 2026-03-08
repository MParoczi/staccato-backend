using AutoMapper;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Repository.Mapping;
using Repository.Repositories;

namespace Tests.Integration.Repositories;

public class PdfExportRepositoryTests
{
    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<EntityToDomainProfile>())
            .CreateMapper();

    private static PdfExportEntity MakeExport(
        Guid notebookId, Guid userId, ExportStatus status, DateTime createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            UserId = userId,
            Status = status,
            CreatedAt = createdAt
        };

    // ── GetExpiredExportsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetExpiredExportsAsync_ReturnsOnlyExportsOlderThanCutoff()
    {
        await using var ctx = CreateContext();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.PdfExports.AddRange(
            MakeExport(notebookId, userId, ExportStatus.Ready,   cutoff.AddDays(-1)),  // older → included
            MakeExport(notebookId, userId, ExportStatus.Pending, cutoff.AddDays(-2)),  // older → included
            MakeExport(notebookId, userId, ExportStatus.Ready,   cutoff.AddDays(+1))   // newer → excluded
        );
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetExpiredExportsAsync(cutoff);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetExpiredExportsAsync_ExcludesFailedExports()
    {
        await using var ctx = CreateContext();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.PdfExports.AddRange(
            MakeExport(notebookId, userId, ExportStatus.Ready,  cutoff.AddDays(-1)),  // older, non-Failed → included
            MakeExport(notebookId, userId, ExportStatus.Failed, cutoff.AddDays(-1))   // older but Failed → excluded
        );
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetExpiredExportsAsync(cutoff);

        Assert.Single(result);
        Assert.NotEqual(ExportStatus.Failed, result[0].Status);
    }

    [Fact]
    public async Task GetExpiredExportsAsync_AllRecent_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ctx.PdfExports.Add(MakeExport(notebookId, userId, ExportStatus.Ready, cutoff.AddDays(+1)));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetExpiredExportsAsync(cutoff);

        Assert.Empty(result);
    }

    // ── GetActiveExportForNotebookAsync ────────────────────────────────────────

    [Fact]
    public async Task GetActiveExportForNotebookAsync_PendingStatus_ReturnsExport()
    {
        await using var ctx = CreateContext();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        ctx.PdfExports.Add(MakeExport(notebookId, userId, ExportStatus.Pending, DateTime.UtcNow));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetActiveExportForNotebookAsync(notebookId);

        Assert.NotNull(result);
        Assert.Equal(ExportStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetActiveExportForNotebookAsync_FailedStatus_ReturnsNull()
    {
        await using var ctx = CreateContext();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        ctx.PdfExports.Add(MakeExport(notebookId, userId, ExportStatus.Failed, DateTime.UtcNow));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetActiveExportForNotebookAsync(notebookId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveExportForNotebookAsync_NoExport_ReturnsNull()
    {
        await using var ctx = CreateContext();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetActiveExportForNotebookAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
