using System.Text.RegularExpressions;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
using DomainModels.Models;
using Microsoft.Extensions.Logging;

namespace Domain.Services;

public partial class PdfExportService(
    IPdfExportRepository pdfExportRepo,
    INotebookRepository notebookRepo,
    ILessonRepository lessonRepo,
    IPdfExportQueue exportQueue,
    IAzureBlobService blobService,
    IUnitOfWork unitOfWork,
    ILogger<PdfExportService> logger) : IPdfExportService
{
    private const int ExpiryHours = 24;

    public async Task<PdfExport> QueueExportAsync(
        Guid userId, Guid notebookId, List<Guid>? lessonIds, CancellationToken ct = default)
    {
        var notebook = await notebookRepo.GetByIdAsync(notebookId, ct)
                       ?? throw new NotFoundException("Notebook not found.");

        if (notebook.UserId != userId)
            throw new ForbiddenException();

        if (await pdfExportRepo.HasActiveExportForNotebookAsync(notebookId, ct))
            throw new ConflictException("ACTIVE_EXPORT_EXISTS", "An active export already exists for this notebook.");

        var deduplicatedLessonIds = lessonIds?.Distinct().ToList();

        if (deduplicatedLessonIds is { Count: > 0 })
        {
            var notebookLessons = await lessonRepo.GetByNotebookIdOrderedByCreatedAtAsync(notebookId, ct);
            var notebookLessonIds = notebookLessons.Select(l => l.Id).ToHashSet();
            var invalidIds = deduplicatedLessonIds.Where(id => !notebookLessonIds.Contains(id)).ToList();
            if (invalidIds.Count > 0)
                throw new BadRequestException("INVALID_LESSON_IDS",
                    "One or more lesson IDs do not belong to this notebook.");
        }

        var export = new PdfExport
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            UserId = userId,
            Status = ExportStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            LessonIds = deduplicatedLessonIds
        };

        await pdfExportRepo.AddAsync(export, ct);
        await unitOfWork.CommitAsync(ct);

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            await exportQueue.EnqueueAsync(export.Id, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError("Queue full — timed out enqueuing export {ExportId}", export.Id);
            throw new ServiceUnavailableException("Export queue is full. Please try again later.");
        }

        logger.LogInformation("Export {ExportId} queued for user {UserId}, notebook {NotebookId}",
            export.Id, userId, notebookId);

        return export;
    }

    public async Task<PdfExport> GetExportByIdAsync(
        Guid exportId, Guid userId, CancellationToken ct = default)
    {
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct)
                     ?? throw new NotFoundException("Export not found.");

        if (export.UserId != userId)
            throw new ForbiddenException();

        return export;
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadExportAsync(
        Guid exportId, Guid userId, CancellationToken ct = default)
    {
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct)
                     ?? throw new NotFoundException("Export not found.");

        if (export.UserId != userId)
            throw new ForbiddenException();

        if (export.Status != ExportStatus.Ready)
            throw new NotFoundException("EXPORT_NOT_READY", "Export is not ready for download.");

        if (export.CompletedAt.HasValue &&
            DateTime.UtcNow > export.CompletedAt.Value.AddHours(ExpiryHours))
            throw new NotFoundException("EXPORT_EXPIRED", "Export has expired.");

        var stream = await blobService.GetStreamAsync(export.BlobReference!, ct)
                     ?? throw new NotFoundException("Export file not found.");

        var notebook = await notebookRepo.GetByIdAsync(export.NotebookId, ct);
        var title = notebook?.Title ?? "Export";
        var fileName = SanitizeFileName(title) + ".pdf";

        return (stream, fileName, "application/pdf");
    }

    public async Task<IReadOnlyList<PdfExport>> GetExportsByUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await pdfExportRepo.GetByUserIdAsync(userId, ct);
    }

    public async Task DeleteExportAsync(
        Guid exportId, Guid userId, CancellationToken ct = default)
    {
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct)
                     ?? throw new NotFoundException("Export not found.");

        if (export.UserId != userId)
            throw new ForbiddenException();

        if (!string.IsNullOrEmpty(export.BlobReference))
            await blobService.DeleteAsync(export.BlobReference, ct);

        pdfExportRepo.Remove(export);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation("Export {ExportId} deleted by user {UserId}", exportId, userId);
    }

    public async Task MarkAsProcessingAsync(Guid exportId, CancellationToken ct = default)
    {
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct);
        if (export is null) return;

        export.Status = ExportStatus.Processing;
        pdfExportRepo.Update(export);
        await unitOfWork.CommitAsync(ct);
    }

    public async Task MarkAsReadyAsync(
        Guid exportId, string blobReference, CancellationToken ct = default)
    {
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct);
        if (export is null) return;

        export.Status = ExportStatus.Ready;
        export.BlobReference = blobReference;
        export.CompletedAt = DateTime.UtcNow;
        pdfExportRepo.Update(export);
        await unitOfWork.CommitAsync(ct);
    }

    public async Task MarkAsFailedAsync(Guid exportId, CancellationToken ct = default)
    {
        var export = await pdfExportRepo.GetByIdAsync(exportId, ct);
        if (export is null) return;

        export.Status = ExportStatus.Failed;
        export.CompletedAt = DateTime.UtcNow;
        pdfExportRepo.Update(export);
        await unitOfWork.CommitAsync(ct);

        logger.LogWarning("Export {ExportId} marked as failed", exportId);
    }

    public async Task<IReadOnlyList<Guid>> ResetStaleProcessingExportsAsync(
        CancellationToken ct = default)
    {
        var staleExports = await pdfExportRepo.GetByStatusAsync(ExportStatus.Processing, ct);
        var exportIds = new List<Guid>();

        foreach (var export in staleExports)
        {
            export.Status = ExportStatus.Pending;
            pdfExportRepo.Update(export);
            exportIds.Add(export.Id);
        }

        if (exportIds.Count > 0)
            await unitOfWork.CommitAsync(ct);

        return exportIds;
    }

    private static string SanitizeFileName(string title)
    {
        var sanitized = FileNameRegex().Replace(title, "_");
        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9 _\-\.()]")]
    private static partial Regex FileNameRegex();
}
