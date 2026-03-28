using Domain.Exceptions;
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
    public async Task<User> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new NotFoundException("User not found.");
        return user;
    }

    public async Task<User> UpdateProfileAsync(Guid userId, string firstName, string lastName, Language language, PageSize? defaultPageSize, Guid? defaultInstrumentId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (defaultInstrumentId is not null)
        {
            var instrument = await instrumentRepository.GetByIdAsync(defaultInstrumentId.Value, ct);
            if (instrument is null)
                throw new NotFoundException("INSTRUMENT_NOT_FOUND", "The specified instrument was not found.");
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Language = language;
        user.DefaultPageSize = defaultPageSize;
        user.DefaultInstrumentId = defaultInstrumentId;

        userRepository.Update(user);
        await unitOfWork.CommitAsync(ct);
        return user;
    }

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
