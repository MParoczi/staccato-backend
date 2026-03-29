using AutoMapper;
using DomainModels.Enums;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.Context;
using Repository.Mapping;
using Repository.Repositories;

namespace Tests.Integration.Repositories;

public class PdfExportRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<EntityToDomainProfile>(), NullLoggerFactory.Instance)
            .CreateMapper();
    }

    private static PdfExportEntity MakeExport(
        Guid notebookId, Guid userId, ExportStatus status, DateTime createdAt)
    {
        return new PdfExportEntity
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            UserId = userId,
            Status = status,
            CreatedAt = createdAt
        };
    }

    // ── GetExpiredExportsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetExpiredExportsAsync_ReturnsOnlyExportsOlderThanCutoff()
    {
        await using var ctx = CreateContext();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Ready export completed >24h before cutoff → included
        var readyExpired = MakeExport(notebookId, userId, ExportStatus.Ready, cutoff.AddDays(-3));
        readyExpired.CompletedAt = cutoff.AddDays(-2); // CompletedAt + 24h = cutoff - 1 day < cutoff
        // Failed export created >24h before cutoff → included
        var failedExpired = MakeExport(notebookId, userId, ExportStatus.Failed, cutoff.AddDays(-2));
        // Ready export completed recently → excluded (CompletedAt + 24h > cutoff)
        var readyRecent = MakeExport(notebookId, userId, ExportStatus.Ready, cutoff.AddDays(-1));
        readyRecent.CompletedAt = cutoff; // CompletedAt + 24h = cutoff + 1 day > cutoff

        ctx.PdfExports.AddRange(readyExpired, failedExpired, readyRecent);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetExpiredExportsAsync(cutoff);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetExpiredExportsAsync_ExcludesPendingAndProcessingExports()
    {
        await using var ctx = CreateContext();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Ready with expired CompletedAt → included
        var readyExport = MakeExport(notebookId, userId, ExportStatus.Ready, cutoff.AddDays(-3));
        readyExport.CompletedAt = cutoff.AddDays(-2);
        // Pending → excluded (still active, not Ready/Failed)
        var pendingExport = MakeExport(notebookId, userId, ExportStatus.Pending, cutoff.AddDays(-3));
        // Processing → excluded (still active, not Ready/Failed)
        var processingExport = MakeExport(notebookId, userId, ExportStatus.Processing, cutoff.AddDays(-3));

        ctx.PdfExports.AddRange(readyExport, pendingExport, processingExport);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var repo = new PdfExportRepository(ctx, CreateMapper());
        var result = await repo.GetExpiredExportsAsync(cutoff);

        Assert.Single(result);
        Assert.Equal(ExportStatus.Ready, result[0].Status);
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