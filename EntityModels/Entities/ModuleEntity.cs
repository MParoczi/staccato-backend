using DomainModels.Enums;

namespace EntityModels.Entities;

public class ModuleEntity : IEntity
{
    public Guid LessonPageId { get; set; }
    public ModuleType ModuleType { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
    public int ZIndex { get; set; }
    public string ContentJson { get; set; } = "[]";

    public LessonPageEntity LessonPage { get; set; } = null!;
    public Guid Id { get; set; }
}