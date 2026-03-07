namespace DomainModels.Models;

public class Lesson
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
