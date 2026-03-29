using Application.Channels;
using Application.Hubs;
using Application.Pdf;
using Domain.Interfaces.Repositories;
using Domain.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.BackgroundServices;

public sealed class PdfExportBackgroundService(
    PdfExportChannel channel,
    IServiceScopeFactory scopeFactory,
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<PdfExportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // FR-025: Recover stale Processing exports on startup
        await RecoverStaleExportsAsync(stoppingToken);

        // Channel reader loop
        await foreach (var exportId in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessExportAsync(exportId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // FR-029: graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing export {ExportId}", exportId);
            }
        }
    }

    private async Task RecoverStaleExportsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var exportService = scope.ServiceProvider.GetRequiredService<IPdfExportService>();

        var recoveredIds = await exportService.ResetStaleProcessingExportsAsync(ct);
        foreach (var id in recoveredIds)
        {
            logger.LogInformation("Recovered stale export {ExportId}, re-enqueuing", id);
            await channel.EnqueueAsync(id, ct);
        }

        if (recoveredIds.Count > 0)
            logger.LogInformation("Recovered {Count} stale exports", recoveredIds.Count);
    }

    private async Task ProcessExportAsync(Guid exportId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var exportService = scope.ServiceProvider.GetRequiredService<IPdfExportService>();
        var dataLoader = scope.ServiceProvider.GetRequiredService<PdfDataLoader>();
        var pdfExportRepo = scope.ServiceProvider.GetRequiredService<IPdfExportRepository>();
        var blobService = scope.ServiceProvider.GetRequiredService<IAzureBlobService>();

        var startTime = DateTime.UtcNow;
        logger.LogInformation("Processing export {ExportId}", exportId);

        // Verify record still exists
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct);
        if (export is null)
        {
            logger.LogWarning("Export {ExportId} not found, skipping", exportId);
            return;
        }

        var userId = export.UserId;

        // Mark as Processing
        await exportService.MarkAsProcessingAsync(exportId, ct);

        try
        {
            // Load all data needed for rendering
            var data = await dataLoader.LoadAsync(exportId, ct);
            if (data is null)
            {
                // FR-028: notebook data missing
                logger.LogWarning("Export {ExportId} data not found, marking as failed", exportId);
                await FailAndNotifyAsync(exportService, exportId, userId, "RENDER_FAILED", ct);
                return;
            }

            // Render PDF
            byte[] pdfBytes;
            try
            {
                var document = new StaccatoPdfDocument(data);
                pdfBytes = document.GeneratePdf();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PDF rendering failed for export {ExportId}", exportId);
                await FailAndNotifyAsync(exportService, exportId, userId, "RENDER_FAILED", ct);
                return;
            }

            // FR-027: Re-check record exists before upload
            var recheck = await pdfExportRepo.GetByIdAsync(exportId, ct);
            if (recheck is null)
            {
                logger.LogWarning("Export {ExportId} deleted during processing, aborting upload", exportId);
                return;
            }

            // Upload to Azure Blob
            var blobPath = $"exports/{userId}/{exportId}.pdf";
            try
            {
                using var stream = new MemoryStream(pdfBytes);
                await blobService.UploadAsync(stream, "application/pdf", blobPath, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Blob upload failed for export {ExportId}", exportId);
                await FailAndNotifyAsync(exportService, exportId, userId, "UPLOAD_FAILED", ct);
                return;
            }

            // Mark as Ready
            await exportService.MarkAsReadyAsync(exportId, blobPath, ct);

            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation("Export {ExportId} completed in {Duration}ms",
                exportId, duration.TotalMilliseconds);

            // Notify via SignalR
            var fileName = SanitizeFileName(data.NotebookTitle) + ".pdf";
            await hubContext.Clients.User(userId.ToString())
                .PdfReady(exportId.ToString(), fileName);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // Let the caller handle shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error for export {ExportId}", exportId);
            await exportService.MarkAsFailedAsync(exportId, ct);
        }
    }

    private async Task FailAndNotifyAsync(
        IPdfExportService exportService, Guid exportId, Guid userId, string errorCode, CancellationToken ct)
    {
        await exportService.MarkAsFailedAsync(exportId, ct);
        await hubContext.Clients.User(userId.ToString())
            .PdfFailed(exportId.ToString(), errorCode);
    }

    private static string SanitizeFileName(string title)
    {
        var sanitized = System.Text.RegularExpressions.Regex.Replace(title, @"[^a-zA-Z0-9 _\-\.()]", "_");
        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }
}
