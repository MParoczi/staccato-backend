using EntityModels;

namespace EntityModels.Entities;

public class LessonEntity : IEntity
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public NotebookEntity Notebook { get; set; } = null!;
    public ICollection<LessonPageEntity> LessonPages { get; set; } = new List<LessonPageEntity>();
}
