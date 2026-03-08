using DomainModels.Models;

namespace Domain.Interfaces.Repositories;

public interface INotebookRepository : IRepository<Notebook>
{
    Task<IReadOnlyList<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the notebook and all 12 of its module styles.
    /// Returns null if no notebook with the given ID exists.
    /// </summary>
    Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)?> GetWithStylesAsync(
        Guid notebookId, CancellationToken ct = default);
}
