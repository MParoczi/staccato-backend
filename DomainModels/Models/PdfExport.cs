using DomainModels.Enums;

namespace DomainModels.Models;

public class PdfExport
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public Guid UserId { get; set; }
    public ExportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? BlobReference { get; set; }
}
