using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IUserSavedPresetRepository : IRepository<UserSavedPreset>
{
    /// <summary>
    ///     Returns all saved presets for the user. Returns an empty list when none exist.
    /// </summary>
    Task<IReadOnlyList<UserSavedPreset>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Returns true if a preset with the given name exists for the user,
    ///     optionally excluding a specific preset (for update uniqueness checks).
    /// </summary>
    Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludePresetId = null, CancellationToken ct = default);
}