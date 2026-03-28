using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public interface IUserService
{
    Task<User> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<User> UpdateProfileAsync(Guid userId, string firstName, string lastName, Language language, PageSize? defaultPageSize, Guid? defaultInstrumentId, CancellationToken ct = default);
    Task ScheduleDeletionAsync(Guid userId, CancellationToken ct = default);
    Task CancelDeletionAsync(Guid userId, CancellationToken ct = default);
    Task<User> UploadAvatarAsync(Guid userId, Stream stream, string contentType, CancellationToken ct = default);
    Task DeleteAvatarAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSavedPreset>> GetPresetsAsync(Guid userId, CancellationToken ct = default);
    Task<UserSavedPreset> CreatePresetAsync(Guid userId, string name, string stylesJson, CancellationToken ct = default);
    Task<UserSavedPreset> UpdatePresetAsync(Guid userId, Guid presetId, string? name, string? stylesJson, CancellationToken ct = default);
    Task DeletePresetAsync(Guid userId, Guid presetId, CancellationToken ct = default);
}
