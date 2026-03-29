using DomainModels.Models;

namespace Domain.Services;

public interface ILessonPageService
{
    Task<IReadOnlyList<LessonPage>> GetByLessonIdAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default);

    Task<(LessonPage Page, bool IsOverSoftLimit)> AddPageAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default);

    Task DeletePageAsync(
        Guid lessonId, Guid pageId, Guid userId, CancellationToken ct = default);
}
