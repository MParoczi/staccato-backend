using DomainModels.Enums;

namespace EntityModels.Entities;

public class NotebookEntity : IEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid InstrumentId { get; set; }
    public PageSize PageSize { get; set; }
    public string CoverColor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
    public InstrumentEntity Instrument { get; set; } = null!;
    public ICollection<LessonEntity> Lessons { get; set; } = new List<LessonEntity>();
    public ICollection<NotebookModuleStyleEntity> ModuleStyles { get; set; } = new List<NotebookModuleStyleEntity>();
    public ICollection<PdfExportEntity> PdfExports { get; set; } = new List<PdfExportEntity>();
    public Guid Id { get; set; }
}