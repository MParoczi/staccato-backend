using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public class NotebookService(
    INotebookRepository notebookRepo,
    INotebookModuleStyleRepository styleRepo,
    ISystemStylePresetRepository systemPresetRepo,
    IUserSavedPresetRepository userPresetRepo,
    IInstrumentRepository instrumentRepo,
    IPdfExportRepository pdfExportRepo,
    IUnitOfWork unitOfWork) : INotebookService
{
    public Task<IReadOnlyList<NotebookSummary>> GetAllByUserAsync(
        Guid userId, CancellationToken ct = default)
        => notebookRepo.GetByUserIdAsync(userId, ct);

    public async Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)> GetByIdAsync(
        Guid notebookId, Guid userId, CancellationToken ct = default)
    {
        var result = await notebookRepo.GetWithStylesAsync(notebookId, ct)
                     ?? throw new NotFoundException("Notebook not found.");
        if (result.Notebook.UserId != userId)
            throw new ForbiddenException();
        return result;
    }

    public async Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)> CreateAsync(
        Guid userId, string title, Guid instrumentId, PageSize pageSize,
        string coverColor, IReadOnlyList<NotebookModuleStyle>? styles,
        CancellationToken ct = default)
    {
        // Validate instrument exists
        var instrument = await instrumentRepo.GetByIdAsync(instrumentId, ct);
        if (instrument is null)
            throw new InstrumentNotFoundException(instrumentId);

        // Resolve styles: use provided or load default Colorful preset
        IReadOnlyList<NotebookModuleStyle> resolvedStyles;
        if (styles is { Count: > 0 })
        {
            resolvedStyles = styles;
        }
        else
        {
            var preset = await systemPresetRepo.GetDefaultAsync(ct)
                         ?? throw new InvalidOperationException(
                             "No default system style preset is configured.");
            resolvedStyles = DeserializePresetStyles(preset.StylesJson, Guid.Empty);
        }

        var notebookId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var notebook = new Notebook
        {
            Id          = notebookId,
            UserId      = userId,
            Title       = title,
            InstrumentId = instrumentId,
            PageSize    = pageSize,
            CoverColor  = coverColor,
            CreatedAt   = now,
            UpdatedAt   = now
        };

        await notebookRepo.AddAsync(notebook, ct);

        foreach (var style in resolvedStyles)
        {
            style.Id         = Guid.NewGuid();
            style.NotebookId = notebookId;
            await styleRepo.AddAsync(style, ct);
        }

        await unitOfWork.CommitAsync(ct);

        return (await notebookRepo.GetWithStylesAsync(notebookId, ct))!.Value;
    }

    public async Task<(Notebook Notebook, IReadOnlyList<NotebookModuleStyle> Styles)> UpdateAsync(
        Guid notebookId, Guid userId, string title, string coverColor,
        CancellationToken ct = default)
    {
        var (notebook, _) = await GetByIdAsync(notebookId, userId, ct);

        notebook.Title      = title;
        notebook.CoverColor = coverColor;
        notebook.UpdatedAt  = DateTime.UtcNow;

        notebookRepo.Update(notebook);
        await unitOfWork.CommitAsync(ct);

        return (await notebookRepo.GetWithStylesAsync(notebookId, ct))!.Value;
    }

    public async Task DeleteAsync(
        Guid notebookId, Guid userId, CancellationToken ct = default)
    {
        var (notebook, _) = await GetByIdAsync(notebookId, userId, ct);

        if (await pdfExportRepo.HasActiveExportForNotebookAsync(notebookId, ct))
            throw new ConflictException("ACTIVE_EXPORT_EXISTS",
                "Cannot delete a notebook with an active PDF export in progress.");

        notebookRepo.Remove(notebook);
        await unitOfWork.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<NotebookModuleStyle>> GetStylesAsync(
        Guid notebookId, Guid userId, CancellationToken ct = default)
    {
        var (_, styles) = await GetByIdAsync(notebookId, userId, ct);
        return styles;
    }

    public async Task<IReadOnlyList<NotebookModuleStyle>> BulkUpdateStylesAsync(
        Guid notebookId, Guid userId, IReadOnlyList<NotebookModuleStyle> styles,
        CancellationToken ct = default)
    {
        var allModuleTypes = Enum.GetValues<ModuleType>().ToHashSet();
        var incomingTypes  = styles.Select(s => s.ModuleType).ToHashSet();

        if (styles.Count != 12 || !incomingTypes.SetEquals(allModuleTypes))
            throw new BadRequestException(
                "INVALID_STYLES",
                "The styles array must contain exactly 12 items, one per ModuleType, with no duplicates.");

        var (notebook, _) = await GetByIdAsync(notebookId, userId, ct);

        var existing = await styleRepo.GetByNotebookIdAsync(notebookId, ct);
        var incoming = styles.ToDictionary(s => s.ModuleType);

        foreach (var record in existing)
        {
            record.StylesJson = incoming[record.ModuleType].StylesJson;
            styleRepo.Update(record);
        }

        notebook.UpdatedAt = DateTime.UtcNow;
        notebookRepo.Update(notebook);

        await unitOfWork.CommitAsync(ct);

        return await styleRepo.GetByNotebookIdAsync(notebookId, ct);
    }

    public async Task<IReadOnlyList<NotebookModuleStyle>> ApplyPresetAsync(
        Guid notebookId, Guid userId, Guid presetId,
        CancellationToken ct = default)
    {
        var (notebook, _) = await GetByIdAsync(notebookId, userId, ct);

        // Look up preset: system first, then user-saved
        var systemPreset = await systemPresetRepo.GetByIdAsync(presetId, ct);
        string stylesJson;

        if (systemPreset is not null)
        {
            stylesJson = systemPreset.StylesJson;
        }
        else
        {
            var userPreset = await userPresetRepo.GetByIdAsync(presetId, ct)
                             ?? throw new NotFoundException("Preset not found.");

            if (userPreset.UserId != userId)
                throw new ForbiddenException();

            stylesJson = userPreset.StylesJson;
        }

        var styleMap = DeserializePresetStyleMap(stylesJson);
        var existing = await styleRepo.GetByNotebookIdAsync(notebookId, ct);

        foreach (var record in existing)
        {
            if (!styleMap.TryGetValue(record.ModuleType.ToString(), out var props))
                throw new InvalidOperationException(
                    $"Preset is missing style definition for module type {record.ModuleType}.");

            record.StylesJson = props;
            styleRepo.Update(record);
        }

        notebook.UpdatedAt = DateTime.UtcNow;
        notebookRepo.Update(notebook);

        await unitOfWork.CommitAsync(ct);

        return await styleRepo.GetByNotebookIdAsync(notebookId, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(
            System.Text.Json.JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    ///     Deserializes a preset StylesJson array into NotebookModuleStyle records.
    ///     The notebookId is set to a placeholder; CreateAsync overwrites it.
    /// </summary>
    private static IReadOnlyList<NotebookModuleStyle> DeserializePresetStyles(
        string stylesJson, Guid notebookId)
    {
        var entries = System.Text.Json.JsonSerializer.Deserialize<
            List<PresetStyleEntry>>(stylesJson, _jsonOptions)!;

        return entries.Select(e => new NotebookModuleStyle
        {
            ModuleType = Enum.Parse<ModuleType>(e.ModuleType, ignoreCase: true),
            NotebookId = notebookId,
            StylesJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                backgroundColor = e.BackgroundColor,
                borderColor     = e.BorderColor,
                borderStyle     = e.BorderStyle,
                borderWidth     = e.BorderWidth,
                borderRadius    = e.BorderRadius,
                headerBgColor   = e.HeaderBgColor,
                headerTextColor = e.HeaderTextColor,
                bodyTextColor   = e.BodyTextColor,
                fontFamily      = e.FontFamily
            }, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            })
        }).ToList();
    }

    /// <summary>
    ///     Deserializes a preset StylesJson array into a dictionary keyed by moduleType string.
    ///     Each value is the serialized flat StylesJson for a NotebookModuleStyleEntity record.
    /// </summary>
    private static Dictionary<string, string> DeserializePresetStyleMap(string stylesJson)
    {
        var entries = System.Text.Json.JsonSerializer.Deserialize<
            List<PresetStyleEntry>>(stylesJson, _jsonOptions)!;

        return entries.ToDictionary(
            e => e.ModuleType,
            e => System.Text.Json.JsonSerializer.Serialize(new
            {
                backgroundColor = e.BackgroundColor,
                borderColor     = e.BorderColor,
                borderStyle     = e.BorderStyle,
                borderWidth     = e.BorderWidth,
                borderRadius    = e.BorderRadius,
                headerBgColor   = e.HeaderBgColor,
                headerTextColor = e.HeaderTextColor,
                bodyTextColor   = e.BodyTextColor,
                fontFamily      = e.FontFamily
            }, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            }),
            StringComparer.OrdinalIgnoreCase);
    }

    private record PresetStyleEntry(
        string ModuleType,
        string BackgroundColor,
        string BorderColor,
        string BorderStyle,
        int BorderWidth,
        int BorderRadius,
        string HeaderBgColor,
        string HeaderTextColor,
        string BodyTextColor,
        string FontFamily);
}
