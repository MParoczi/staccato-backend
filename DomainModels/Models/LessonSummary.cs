namespace DomainModels.Models;

public class LessonSummary
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int PageCount { get; set; }
}