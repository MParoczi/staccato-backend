namespace DomainModels.Models;

public class LessonPage
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public int PageNumber { get; set; }
    public int ModuleCount { get; set; }
}