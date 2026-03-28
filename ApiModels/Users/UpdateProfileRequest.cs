namespace ApiModels.Users;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string Language,
    string? DefaultPageSize,
    Guid? DefaultInstrumentId);
