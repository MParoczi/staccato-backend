using DomainModels.Models;

namespace Domain.Services;

public interface IPdfExportService
{
    Task<PdfExport> QueueExportAsync(
        Guid userId,
        Guid notebookId,
        List<Guid>? lessonIds,
        CancellationToken ct = default);

    Task<PdfExport> GetExportByIdAsync(
        Guid exportId,
        Guid userId,
        CancellationToken ct = default);

    Task<(Stream Content, string FileName, string ContentType)> DownloadExportAsync(
        Guid exportId,
        Guid userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<PdfExport>> GetExportsByUserAsync(
        Guid userId,
        CancellationToken ct = default);

    Task DeleteExportAsync(
        Guid exportId,
        Guid userId,
        CancellationToken ct = default);

    Task MarkAsProcessingAsync(
        Guid exportId,
        CancellationToken ct = default);

    Task MarkAsReadyAsync(
        Guid exportId,
        string blobReference,
        CancellationToken ct = default);

    Task MarkAsFailedAsync(
        Guid exportId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> ResetStaleProcessingExportsAsync(
        CancellationToken ct = default);
}
