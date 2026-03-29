namespace ApiModels.Exports;

public record PdfExportResponse(
    Guid Id,
    Guid NotebookId,
    string NotebookTitle,
    string Status,
    string CreatedAt,
    string? CompletedAt,
    List<Guid>? LessonIds);
