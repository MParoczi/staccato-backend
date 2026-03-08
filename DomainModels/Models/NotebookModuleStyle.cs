using DomainModels.Enums;

namespace DomainModels.Models;

public class NotebookModuleStyle
{
    public Guid Id { get; set; }
    public Guid NotebookId { get; set; }
    public ModuleType ModuleType { get; set; }
    public string StylesJson { get; set; } = string.Empty;
}