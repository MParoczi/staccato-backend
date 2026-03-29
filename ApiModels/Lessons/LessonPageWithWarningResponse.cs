namespace ApiModels.Lessons;

public record LessonPageWithWarningResponse(
    LessonPageResponse Data,
    string? Warning
);