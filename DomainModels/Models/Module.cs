using DomainModels.Enums;

namespace DomainModels.Models;

public class Module
{
    public Guid Id { get; set; }
    public Guid LessonPageId { get; set; }
    public ModuleType ModuleType { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
    public string ContentJson { get; set; } = "[]";
}