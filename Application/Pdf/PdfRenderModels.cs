using DomainModels.BuildingBlocks;
using DomainModels.Enums;
using DomainModels.Models;

namespace Application.Pdf;

public record PdfExportData(
    string NotebookTitle,
    string CoverColor,
    PageSize PageSize,
    DateTime CreatedAt,
    string OwnerName,
    Language Language,
    string InstrumentName,
    int InstrumentStringCount,
    IReadOnlyDictionary<ModuleType, ModuleStyleData> Styles,
    IReadOnlyList<LessonRenderData> Lessons,
    IReadOnlyDictionary<Guid, Chord> Chords);

public record LessonRenderData(
    string Title,
    IReadOnlyList<PageRenderData> Pages);

public record PageRenderData(
    int PageNumber,
    IReadOnlyList<ModuleRenderData> Modules);

public record ModuleRenderData(
    ModuleType ModuleType,
    int GridX,
    int GridY,
    int GridWidth,
    int GridHeight,
    int ZIndex,
    IReadOnlyList<BuildingBlock> BuildingBlocks);

public record ModuleStyleData(
    string BackgroundColor,
    string BorderColor,
    int BorderWidth,
    int BorderRadius,
    string HeaderBgColor,
    string HeaderTextColor,
    string BodyTextColor,
    string FontFamily);
