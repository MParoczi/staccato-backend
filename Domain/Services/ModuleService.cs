using System.Text.Json;
using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Constants;
using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public class ModuleService(
    IModuleRepository moduleRepo,
    ILessonPageRepository lessonPageRepo,
    ILessonRepository lessonRepo,
    INotebookRepository notebookRepo,
    IUnitOfWork unitOfWork) : IModuleService
{
    public Task<IReadOnlyList<Module>> GetModulesByPageIdAsync(
        Guid pageId, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Module> CreateModuleAsync(
        Guid pageId, ModuleType moduleType,
        int gridX, int gridY, int gridWidth, int gridHeight, int zIndex,
        string contentJson, Guid userId, CancellationToken ct = default)
    {
        var (page, lesson, notebook) = await VerifyPageOwnershipAsync(pageId, userId, ct);

        // FR-015: content must be empty array on POST
        var content = JsonSerializer.Deserialize<JsonElement>(contentJson);
        if (content.ValueKind != JsonValueKind.Array || content.GetArrayLength() != 0)
            throw new ValidationException("BREADCRUMB_CONTENT_NOT_EMPTY",
                "Content must be an empty array when creating a module.");

        // FR-010: Breadcrumb must always have empty content
        if (moduleType == ModuleType.Breadcrumb)
            ValidateContentAsync(moduleType, contentJson);

        // FR-011: only one Title per lesson
        if (moduleType == ModuleType.Title)
        {
            var hasTitleModule = await moduleRepo.HasTitleModuleInLessonAsync(lesson.Id, null, ct);
            if (hasTitleModule)
                throw new ConflictException("DUPLICATE_TITLE_MODULE",
                    "Only one Title module is allowed per lesson.");
        }

        // FR-006, FR-007, FR-008: grid placement validation
        await ValidateGridPlacementAsync(pageId, notebook.PageSize, moduleType,
            gridX, gridY, gridWidth, gridHeight, null, ct);

        var module = new Module
        {
            Id = Guid.NewGuid(),
            LessonPageId = pageId,
            ModuleType = moduleType,
            GridX = gridX,
            GridY = gridY,
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            ZIndex = zIndex,
            ContentJson = contentJson
        };

        await moduleRepo.AddAsync(module, ct);
        await unitOfWork.CommitAsync(ct);

        return module;
    }

    public Task<Module> UpdateModuleAsync(
        Guid moduleId, ModuleType moduleType,
        int gridX, int gridY, int gridWidth, int gridHeight, int zIndex,
        string contentJson, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Module> UpdateModuleLayoutAsync(
        Guid moduleId, int gridX, int gridY, int gridWidth, int gridHeight, int zIndex,
        Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteModuleAsync(
        Guid moduleId, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private async Task<(LessonPage Page, Lesson Lesson, Notebook Notebook)> VerifyPageOwnershipAsync(
        Guid pageId, Guid userId, CancellationToken ct)
    {
        var page = await lessonPageRepo.GetByIdAsync(pageId, ct)
                   ?? throw new NotFoundException();

        var lesson = await lessonRepo.GetByIdAsync(page.LessonId, ct)
                     ?? throw new NotFoundException();

        var notebook = await notebookRepo.GetByIdAsync(lesson.NotebookId, ct)
                       ?? throw new NotFoundException();

        if (notebook.UserId != userId)
            throw new ForbiddenException();

        return (page, lesson, notebook);
    }

    private async Task<(Module Module, LessonPage Page, Lesson Lesson, Notebook Notebook)> VerifyModuleOwnershipAsync(
        Guid moduleId, Guid userId, CancellationToken ct)
    {
        var module = await moduleRepo.GetByIdAsync(moduleId, ct)
                     ?? throw new NotFoundException();

        var (page, lesson, notebook) = await VerifyPageOwnershipAsync(module.LessonPageId, userId, ct);
        return (module, page, lesson, notebook);
    }

    private async Task ValidateGridPlacementAsync(
        Guid pageId, PageSize pageSize, ModuleType moduleType,
        int gridX, int gridY, int gridWidth, int gridHeight,
        Guid? excludeModuleId, CancellationToken ct)
    {
        var (minWidth, minHeight) = ModuleTypeConstraints.MinimumSizes[moduleType];
        if (gridWidth < minWidth || gridHeight < minHeight)
            throw new ValidationException("MODULE_TOO_SMALL",
                $"Module must be at least {minWidth}x{minHeight} for type {moduleType}.");

        var (_, _, pageGridWidth, pageGridHeight) = PageSizeDimensions.Dimensions[pageSize];
        if (gridX + gridWidth > pageGridWidth || gridY + gridHeight > pageGridHeight)
            throw new ValidationException("MODULE_OUT_OF_BOUNDS",
                "Module extends beyond the page boundary.");

        var overlaps = await moduleRepo.CheckOverlapAsync(pageId, gridX, gridY, gridWidth, gridHeight,
            excludeModuleId, ct);
        if (overlaps)
            throw new ValidationException("MODULE_OVERLAP",
                "The module overlaps with an existing module on this page.");
    }

    private static void ValidateContentAsync(ModuleType moduleType, string contentJson)
    {
        if (moduleType == ModuleType.Breadcrumb)
        {
            var breadcrumbArray = JsonSerializer.Deserialize<JsonElement>(contentJson);
            if (breadcrumbArray.ValueKind != JsonValueKind.Array || breadcrumbArray.GetArrayLength() != 0)
                throw new ValidationException("BREADCRUMB_CONTENT_NOT_EMPTY",
                    "Breadcrumb modules must have empty content.");
            return;
        }

        JsonElement array;
        try
        {
            array = JsonSerializer.Deserialize<JsonElement>(contentJson);
        }
        catch (JsonException)
        {
            throw new BadRequestException("MALFORMED_CONTENT_JSON",
                "Content contains malformed JSON.");
        }

        if (array.ValueKind != JsonValueKind.Array)
            throw new BadRequestException("MALFORMED_CONTENT_JSON",
                "Content must be a JSON array.");

        var allowedBlocks = ModuleTypeConstraints.AllowedBlocks[moduleType];

        foreach (var block in array.EnumerateArray())
        {
            if (!block.TryGetProperty("type", out var typeProp))
                throw new ValidationException("INVALID_BUILDING_BLOCK",
                    "Each building block must have a 'type' property.");

            var typeString = typeProp.GetString();
            if (typeString == null || !Enum.TryParse<BuildingBlockType>(typeString, true, out var blockType))
                throw new ValidationException("INVALID_BUILDING_BLOCK",
                    $"Unrecognized building block type: '{typeString}'.");

            if (!allowedBlocks.Contains(blockType))
                throw new ValidationException("INVALID_BUILDING_BLOCK",
                    $"Building block type '{blockType}' is not allowed in {moduleType} modules.");
        }
    }
}
