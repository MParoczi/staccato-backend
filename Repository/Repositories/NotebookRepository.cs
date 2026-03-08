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
    public async Task<IReadOnlyList<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var entities = await _context.Notebooks
            .Where(n => n.UserId == userId)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<Notebook>>(entities);
    }

    public async Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)?> GetWithStylesAsync(
        Guid notebookId, CancellationToken ct = default)
    {
        var entity = await _context.Notebooks
            .Include(n => n.ModuleStyles)
            .FirstOrDefaultAsync(n => n.Id == notebookId, ct);

        if (entity is null) return null;

        var notebook = _mapper.Map<Notebook>(entity);
        var styles = _mapper.Map<IReadOnlyList<NotebookModuleStyle>>(entity.ModuleStyles);
        return (notebook, styles);
    }
}
