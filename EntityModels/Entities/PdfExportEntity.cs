using DomainModels.Enums;

namespace EntityModels.Entities;

public class PdfExportEntity
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public Guid UserId { get; set; }
    public ExportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? BlobReference { get; set; }
    public string? LessonIdsJson { get; set; }

    public NotebookEntity Notebook { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
}
