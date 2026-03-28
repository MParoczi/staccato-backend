using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class NotebookRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<NotebookEntity, Notebook>(context, mapper), INotebookRepository
{
    public async Task<IReadOnlyList<NotebookSummary>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        return await _context.Notebooks
            .Include(n => n.Instrument)
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new NotebookSummary
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                InstrumentName = n.Instrument.DisplayName,
                PageSize = n.PageSize,
                CoverColor = n.CoverColor,
                LessonCount = n.Lessons.Count,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)?> GetWithStylesAsync(
        Guid notebookId, CancellationToken ct = default)
    {
        var entity = await _context.Notebooks
            .AsNoTracking()
            .Include(n => n.Instrument)
            .Include(n => n.Lessons)
            .Include(n => n.ModuleStyles)
            .FirstOrDefaultAsync(n => n.Id == notebookId, ct);

        if (entity is null) return null;

        var notebook = _mapper.Map<Notebook>(entity);
        var styles = _mapper.Map<IReadOnlyList<NotebookModuleStyle>>(
            entity.ModuleStyles.OrderBy(s => s.ModuleType).ToList());
        return (notebook, styles);
    }
}