using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public interface INotebookService
{
    Task<IReadOnlyList<NotebookSummary>> GetAllByUserAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)> GetByIdAsync(
        Guid notebookId,
        Guid userId,
        CancellationToken ct = default);

    Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)> CreateAsync(
        Guid userId,
        string title,
        Guid instrumentId,
        PageSize pageSize,
        string coverColor,
        IReadOnlyList<NotebookModuleStyle>? styles,
        CancellationToken ct = default);

    Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)> UpdateAsync(
        Guid notebookId,
        Guid userId,
        string title,
        string coverColor,
        CancellationToken ct = default);

    Task DeleteAsync(
        Guid notebookId,
        Guid userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<NotebookModuleStyle>> GetStylesAsync(
        Guid notebookId,
        Guid userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<NotebookModuleStyle>> BulkUpdateStylesAsync(
        Guid notebookId,
        Guid userId,
        IReadOnlyList<NotebookModuleStyle> styles,
        CancellationToken ct = default);

    Task<IReadOnlyList<NotebookModuleStyle>> ApplyPresetAsync(
        Guid notebookId,
        Guid userId,
        Guid presetId,
        CancellationToken ct = default);
}