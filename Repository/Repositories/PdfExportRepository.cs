using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class PdfExportRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<PdfExportEntity, PdfExport>(context, mapper), IPdfExportRepository
{
    public async Task<PdfExport?> GetActiveExportForNotebookAsync(
        Guid notebookId, CancellationToken ct = default)
    {
        var entity = await _context.PdfExports
            .FirstOrDefaultAsync(e =>
                e.NotebookId == notebookId && e.Status != ExportStatus.Failed, ct);
        return _mapper.Map<PdfExport?>(entity);
    }

    public async Task<IReadOnlyList<PdfExport>> GetExpiredExportsAsync(
        DateTime utcCutoff, CancellationToken ct = default)
    {
        var entities = await _context.PdfExports
            .Where(e => e.Status != ExportStatus.Failed && e.CreatedAt < utcCutoff)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<PdfExport>>(entities);
    }

    public async Task<IReadOnlyList<PdfExport>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var entities = await _context.PdfExports
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<PdfExport>>(entities);
    }
}
