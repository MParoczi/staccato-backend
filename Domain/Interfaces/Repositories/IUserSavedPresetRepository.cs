using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface IUserSavedPresetRepository : IRepository<UserSavedPreset>
{
    /// <summary>
    ///     Returns all saved presets for the user. Returns an empty list when none exist.
    /// </summary>
    Task<IReadOnlyList<UserSavedPreset>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}