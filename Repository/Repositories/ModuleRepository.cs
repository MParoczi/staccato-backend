using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class ModuleRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<ModuleEntity, Module>(context, mapper), IModuleRepository
{
    public async Task<IReadOnlyList<Module>> GetByPageIdAsync(Guid pageId, CancellationToken ct = default)
    {
        var entities = await _context.Modules
            .Where(m => m.LessonPageId == pageId)
            .OrderBy(m => m.GridY)
            .ThenBy(m => m.GridX)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<Module>>(entities);
    }

    public Task<bool> CheckOverlapAsync(
        Guid pageId,
        int gridX, int gridY, int gridWidth, int gridHeight,
        Guid? excludeModuleId = null,
        CancellationToken ct = default)
    {
        return _context.Modules
            .Where(m => m.LessonPageId == pageId)
            .Where(m => excludeModuleId == null || m.Id != excludeModuleId)
            .AnyAsync(m =>
                    m.GridX < gridX + gridWidth && m.GridX + m.GridWidth > gridX &&
                    m.GridY < gridY + gridHeight && m.GridY + m.GridHeight > gridY,
                ct);
    }
}