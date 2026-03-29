using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IModuleRepository : IRepository<Module>
{
    Task<IReadOnlyList<Module>> GetByPageIdAsync(Guid pageId, CancellationToken ct = default);

    /// <summary>
    ///     Returns true if the proposed rectangle overlaps any existing module on the page.
    ///     When excludeModuleId is provided, that module is excluded from the check (update scenario).
    ///     Returns false when the page has no modules (or only the excluded module).
    /// </summary>
    Task<bool> CheckOverlapAsync(
        Guid pageId,
        int gridX, int gridY, int gridWidth, int gridHeight,
        Guid? excludeModuleId = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Returns true if any module of type Title exists across all pages of the specified lesson.
    ///     When excludeModuleId is provided, that module is excluded from the check.
    /// </summary>
    Task<bool> HasTitleModuleInLessonAsync(Guid lessonId, Guid? excludeModuleId = null, CancellationToken ct = default);
}