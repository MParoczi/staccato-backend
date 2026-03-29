using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public interface IModuleService
{
    Task<IReadOnlyList<Module>> GetModulesByPageIdAsync(
        Guid pageId, Guid userId, CancellationToken ct = default);

    Task<Module> CreateModuleAsync(
        Guid pageId, ModuleType moduleType,
        int gridX, int gridY, int gridWidth, int gridHeight, int zIndex,
        string contentJson, Guid userId, CancellationToken ct = default);

    Task<Module> UpdateModuleAsync(
        Guid moduleId, ModuleType moduleType,
        int gridX, int gridY, int gridWidth, int gridHeight, int zIndex,
        string contentJson, Guid userId, CancellationToken ct = default);

    Task<Module> UpdateModuleLayoutAsync(
        Guid moduleId, int gridX, int gridY, int gridWidth, int gridHeight, int zIndex,
        Guid userId, CancellationToken ct = default);

    Task DeleteModuleAsync(
        Guid moduleId, Guid userId, CancellationToken ct = default);
}
