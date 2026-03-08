using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface ILessonRepository : IRepository<Lesson>
{
    /// <summary>
    /// Returns all lessons for the notebook ordered by CreatedAt ascending.
    /// Returns an empty list when the notebook has no lessons.
    /// </summary>
    Task<IReadOnlyList<Lesson>> GetByNotebookIdOrderedByCreatedAtAsync(
        Guid notebookId, CancellationToken ct = default);

    /// <summary>
    /// Returns the lesson and its pages ordered by PageNumber ascending.
    /// Returns null if no lesson with the given ID exists.
    /// </summary>
    Task<(Lesson Lesson, IReadOnlyList<LessonPage> Pages)?> GetWithPagesAsync(
        Guid lessonId, CancellationToken ct = default);
}
