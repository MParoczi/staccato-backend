using AutoMapper;
using Domain.Interfaces.Repositories;
using DomainModels.Models;
using EntityModels.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Repository.Repositories;

public class LessonRepository(AppDbContext context, IMapper mapper)
    : RepositoryBase<LessonEntity, Lesson>(context, mapper), ILessonRepository
{
    public async Task<IReadOnlyList<Lesson>> GetByNotebookIdOrderedByCreatedAtAsync(
        Guid notebookId, CancellationToken ct = default)
    {
        var entities = await _context.Lessons
            .Where(l => l.NotebookId == notebookId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);
        return _mapper.Map<IReadOnlyList<Lesson>>(entities);
    }

    public async Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)?> GetWithPagesAsync(
        Guid lessonId, CancellationToken ct = default)
    {
        var entity = await _context.Lessons
            .Include(l => l.LessonPages.OrderBy(p => p.PageNumber))
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (entity is null) return null;

        var lesson = _mapper.Map<Lesson>(entity);
        var pages = _mapper.Map<IReadOnlyList<LessonPage>>(entity.LessonPages);
        return (lesson, pages);
    }
}
