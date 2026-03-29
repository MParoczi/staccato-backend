namespace ApiModels.Lessons;

public record NotebookIndexEntryResponse(
    Guid LessonId,
    string Title,
    string CreatedAt,
    int StartPageNumber
);