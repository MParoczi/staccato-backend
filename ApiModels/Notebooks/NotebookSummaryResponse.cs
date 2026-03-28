namespace ApiModels.Notebooks;

public record NotebookSummaryResponse(
    Guid Id,
    string Title,
    string InstrumentName,
    string PageSize,
    string CoverColor,
    int LessonCount,
    string CreatedAt,
    string UpdatedAt
);
