namespace EntityModels.Entities;

public class LessonPageEntity
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public int PageNumber { get; set; }

    public LessonEntity Lesson { get; set; } = null!;
    public ICollection<ModuleEntity> Modules { get; set; } = new List<ModuleEntity>();
}
