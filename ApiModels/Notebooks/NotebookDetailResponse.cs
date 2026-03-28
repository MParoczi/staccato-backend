namespace ApiModels.Notebooks;

public record NotebookDetailResponse(
    Guid Id,
    string Title,
    Guid InstrumentId,
    string InstrumentName,
    string PageSize,
    string CoverColor,
    int LessonCount,
    string CreatedAt,
    string UpdatedAt,
    List<ModuleStyleResponse> Styles
);