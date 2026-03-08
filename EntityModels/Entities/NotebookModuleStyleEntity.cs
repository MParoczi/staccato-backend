using DomainModels.Enums;

namespace EntityModels.Entities;

public class NotebookModuleStyleEntity : IEntity
{
    public Guid NotebookId { get; set; }
    public ModuleType ModuleType { get; set; }
    public string StylesJson { get; set; } = string.Empty;

    public NotebookEntity Notebook { get; set; } = null!;
    public Guid Id { get; set; }
}