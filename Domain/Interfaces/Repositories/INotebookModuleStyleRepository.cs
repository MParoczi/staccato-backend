using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface INotebookModuleStyleRepository : IRepository<NotebookModuleStyle>
{
    /// <summary>
    /// Returns all 12 styles for the notebook ordered by ModuleType (enum integer value ascending).
    /// </summary>
    Task<IReadOnlyList<NotebookModuleStyle>> GetByNotebookIdAsync(
        Guid notebookId, CancellationToken ct = default);

    Task<NotebookModuleStyle?> GetByNotebookIdAndTypeAsync(
        Guid notebookId, ModuleType moduleType, CancellationToken ct = default);
}
