using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class NotebookModuleStyleRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<NotebookModuleStyleEntity, NotebookModuleStyle>(context, mapper), INotebookModuleStyleRepository
{
    public async Task<IReadOnlyList<NotebookModuleStyle>> GetByNotebookIdAsync(
        Guid notebookId, CancellationToken ct = default)
    {
        var entities = await _context.NotebookModuleStyles
            .AsNoTracking()
            .Where(s => s.NotebookId == notebookId)
            .OrderBy(s => (int)s.ModuleType)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<NotebookModuleStyle>>(entities);
    }

    public async Task<NotebookModuleStyle?> GetByNotebookIdAndTypeAsync(
        Guid notebookId, ModuleType moduleType, CancellationToken ct = default)
    {
        var entity = await _context.NotebookModuleStyles
            .FirstOrDefaultAsync(s => s.NotebookId == notebookId && s.ModuleType == moduleType, ct);
        return _mapper.Map<NotebookModuleStyle?>(entity);
    }
}