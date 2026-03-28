using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
using DomainModels.Models;

namespace Domain.Services;

public class UserService(
    IUserRepository userRepository,
    IUserSavedPresetRepository presetRepository,
    IInstrumentRepository instrumentRepository,
    IAzureBlobService blobService,
    IUnitOfWork unitOfWork) : IUserService
{
    public Task<User> GetProfileAsync(Guid userId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<User> UpdateProfileAsync(Guid userId, string firstName, string lastName, Language language, PageSize? defaultPageSize, Guid? defaultInstrumentId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task ScheduleDeletionAsync(Guid userId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task CancelDeletionAsync(Guid userId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<User> UploadAvatarAsync(Guid userId, Stream stream, string contentType, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DeleteAvatarAsync(Guid userId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<UserSavedPreset>> GetPresetsAsync(Guid userId, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<UserSavedPreset> CreatePresetAsync(Guid userId, string name, string stylesJson, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<UserSavedPreset> UpdatePresetAsync(Guid userId, Guid presetId, string? name, string? stylesJson, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DeletePresetAsync(Guid userId, Guid presetId, CancellationToken ct = default)
        => throw new NotImplementedException();
}
