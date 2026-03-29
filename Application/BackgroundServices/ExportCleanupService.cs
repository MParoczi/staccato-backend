using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;

namespace Application.BackgroundServices;

public sealed class ExportCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExportCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (await timer.WaitForNextTickAsync(stoppingToken)) await RunCleanupAsync(stoppingToken);
    }

    internal async Task RunCleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var exportRepo = scope.ServiceProvider.GetRequiredService<IPdfExportRepository>();
        var blobService = scope.ServiceProvider.GetRequiredService<IAzureBlobService>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyList<DomainModels.Models.PdfExport> expiredExports;
        try
        {
            expiredExports = await exportRepo.GetExpiredExportsAsync(DateTime.UtcNow, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query expired exports for cleanup");
            return;
        }

        if (expiredExports.Count == 0)
            return;

        foreach (var export in expiredExports)
        {
            if (!string.IsNullOrEmpty(export.BlobReference))
                try
                {
                    await blobService.DeleteAsync(export.BlobReference, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to delete blob for export {ExportId}. Continuing", export.Id);
                }

            exportRepo.Remove(export);
        }

        try
        {
            await uow.CommitAsync(ct);
            logger.LogInformation("Cleaned up {Count} expired export(s)", expiredExports.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to commit export cleanup batch. Exports will be retried on the next run");
        }
    }
}
