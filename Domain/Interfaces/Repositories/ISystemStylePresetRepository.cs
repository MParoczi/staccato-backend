using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface ISystemStylePresetRepository : IRepository<SystemStylePreset>
{
    /// <summary>
    ///     Returns all system style presets ordered by DisplayOrder ascending.
    ///     Used by GET /presets.
    /// </summary>
    Task<IReadOnlyList<SystemStylePreset>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    ///     Returns the preset where IsDefault = true, or null if none is configured.
    ///     Used by NotebookService.CreateAsync to avoid loading all 5 presets when only the default is needed.
    /// </summary>
    Task<SystemStylePreset?> GetDefaultAsync(CancellationToken ct = default);
}
