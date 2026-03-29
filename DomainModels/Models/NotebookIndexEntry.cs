namespace DomainModels.Models;

public class NotebookIndexEntry
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int StartPageNumber { get; set; }
}