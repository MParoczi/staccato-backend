using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IPdfExportRepository : IRepository<PdfExport>
{
    /// <summary>
    /// Returns the active export (Pending, Processing, or Ready) for the given notebook,
    /// or null if none exists.
    /// </summary>
    Task<PdfExport?> GetActiveExportForNotebookAsync(Guid notebookId, CancellationToken ct = default);

    /// <summary>
    /// Returns all non-Failed exports whose CreatedAt is strictly older than utcCutoff.
    /// The repository must NOT call DateTime.UtcNow internally.
    /// </summary>
    Task<IReadOnlyList<PdfExport>> GetExpiredExportsAsync(DateTime utcCutoff, CancellationToken ct = default);

    /// <summary>
    /// Returns all exports for the user ordered by CreatedAt descending.
    /// </summary>
    Task<IReadOnlyList<PdfExport>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
