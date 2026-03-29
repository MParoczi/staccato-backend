namespace ApiModels.Lessons;

public record LessonDetailResponse(
    Guid Id,
    Guid NotebookId,
    string Title,
    string CreatedAt,
    List<LessonPageResponse> Pages
);