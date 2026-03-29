using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class LessonPageRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<LessonPageEntity, LessonPage>(context, mapper), ILessonPageRepository
{
    public async Task<IReadOnlyList<LessonPage>> GetByLessonIdOrderedAsync(
        Guid lessonId, CancellationToken ct = default)
    {
        return await _context.LessonPages
            .Where(p => p.LessonId == lessonId)
            .OrderBy(p => p.PageNumber)
            .Select(p => new LessonPage
            {
                Id = p.Id,
                LessonId = p.LessonId,
                PageNumber = p.PageNumber,
                ModuleCount = p.Modules.Count
            })
            .ToListAsync(ct);
    }

    public async Task<(LessonPage Page, IReadOnlyList<Module> Modules)?> GetPageWithModulesAsync(
        Guid pageId, CancellationToken ct = default)
    {
        var entity = await _context.LessonPages
            .Include(p => p.Modules.OrderBy(m => m.GridY).ThenBy(m => m.GridX))
            .FirstOrDefaultAsync(p => p.Id == pageId, ct);

        if (entity is null) return null;

        var page = _mapper.Map<LessonPage>(entity);
        page.ModuleCount = entity.Modules.Count;
        var modules = _mapper.Map<IReadOnlyList<Module>>(entity.Modules);
        return (page, modules);
    }

    public async Task<int> GetPageCountByLessonIdAsync(Guid lessonId, CancellationToken ct = default)
    {
        return await _context.LessonPages
            .Where(p => p.LessonId == lessonId)
            .CountAsync(ct);
    }

    public async Task<int> GetMaxPageNumberByLessonIdAsync(Guid lessonId, CancellationToken ct = default)
    {
        var hasPages = await _context.LessonPages
            .AnyAsync(p => p.LessonId == lessonId, ct);

        if (!hasPages) return 0;

        return await _context.LessonPages
            .Where(p => p.LessonId == lessonId)
            .MaxAsync(p => p.PageNumber, ct);
    }
}