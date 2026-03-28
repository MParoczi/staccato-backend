using DomainModels.Enums;

namespace DomainModels.Models;

public class NotebookSummary
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string InstrumentName { get; set; } = string.Empty;
    public PageSize PageSize { get; set; }
    public string CoverColor { get; set; } = string.Empty;
    public int LessonCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
