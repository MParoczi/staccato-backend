namespace ApiModels.Lessons;

public record LessonPageResponse(
    Guid Id,
    Guid LessonId,
    int PageNumber,
    int ModuleCount
);
