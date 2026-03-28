using DomainModels.Enums;

namespace EntityModels.Entities;

public class UserEntity : IEntity
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledDeletionAt { get; set; }
    public Language Language { get; set; }
    public PageSize? DefaultPageSize { get; set; }
    public Guid? DefaultInstrumentId { get; set; }
    public InstrumentEntity? DefaultInstrument { get; set; }

    public ICollection<NotebookEntity> Notebooks { get; set; } = new List<NotebookEntity>();
    public ICollection<RefreshTokenEntity> RefreshTokens { get; set; } = new List<RefreshTokenEntity>();
    public ICollection<UserSavedPresetEntity> UserSavedPresets { get; set; } = new List<UserSavedPresetEntity>();
    public ICollection<PdfExportEntity> PdfExports { get; set; } = new List<PdfExportEntity>();
    public Guid Id { get; set; }
}