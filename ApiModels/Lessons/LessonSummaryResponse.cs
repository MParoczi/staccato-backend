namespace ApiModels.Lessons;

public record LessonSummaryResponse(
    Guid Id,
    string Title,
    string CreatedAt,
    int PageCount
);