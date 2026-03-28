using DomainModels.Enums;

namespace DomainModels.Models;

public class User
{
    public Guid Id { get; set; }
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
}