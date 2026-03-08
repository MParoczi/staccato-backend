using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface ILessonPageRepository : IRepository<LessonPage>
{
    /// <summary>
    ///     Returns all pages for the lesson ordered by PageNumber ascending.
    ///     Returns an empty list when the lesson has no pages.
    /// </summary>
    Task<IReadOnlyList<LessonPage>> GetByLessonIdOrderedAsync(
        Guid lessonId, CancellationToken ct = default);

    /// <summary>
    ///     Returns the page and all its modules ordered by GridY ascending, then GridX ascending.
    ///     Returns null if no page with the given ID exists.
    /// </summary>
    Task<(LessonPage Page, IReadOnlyList<Module> Modules)?> GetPageWithModulesAsync(
        Guid pageId, CancellationToken ct = default);
}