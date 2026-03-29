using DomainModels.Models;

namespace Domain.Services;

public interface ILessonService
{
    Task<IReadOnlyList<LessonSummary>> GetByNotebookIdAsync(
        Guid notebookId, Guid userId, CancellationToken ct = default);

    Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)> CreateAsync(
        Guid notebookId, Guid userId, string title, CancellationToken ct = default);

    Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)> GetByIdAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default);

    Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)> UpdateAsync(
        Guid lessonId, Guid userId, string title, CancellationToken ct = default);

    Task DeleteAsync(
        Guid lessonId, Guid userId, CancellationToken ct = default);
}
