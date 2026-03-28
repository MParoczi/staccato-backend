namespace ApiModels.Users;

public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Language,
    string? DefaultPageSize,
    Guid? DefaultInstrumentId,
    string? AvatarUrl,
    DateTime? ScheduledDeletionAt);
