namespace ApiModels.Exports;

public class CreatePdfExportRequest
{
    public Guid NotebookId { get; set; }
    public List<Guid>? LessonIds { get; set; }
}
