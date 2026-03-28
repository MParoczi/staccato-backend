using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using DomainModels.Enums;
using DomainModels.Models;
using Microsoft.Extensions.Logging;

namespace Domain.Services;

public class UserService(
    IUserRepository userRepository,
    IUserSavedPresetRepository presetRepository,
    IInstrumentRepository instrumentRepository,
    IAzureBlobService blobService,
    IUnitOfWork unitOfWork,
    ILogger<UserService> logger) : IUserService
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

    public async Task ScheduleDeletionAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (user.ScheduledDeletionAt is not null)
            throw new ConflictException("ACCOUNT_DELETION_ALREADY_SCHEDULED",
                "Account is already scheduled for deletion.");

        user.ScheduledDeletionAt = DateTime.UtcNow.AddDays(30);
        userRepository.Update(user);
        await unitOfWork.CommitAsync(ct);
    }

    public async Task CancelDeletionAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (user.ScheduledDeletionAt is null)
            throw new BadRequestException("ACCOUNT_DELETION_NOT_SCHEDULED",
                "Account is not scheduled for deletion.");

        user.ScheduledDeletionAt = null;
        userRepository.Update(user);
        await unitOfWork.CommitAsync(ct);
    }

    public async Task<User> UploadAvatarAsync(Guid userId, Stream stream, string contentType, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (user.AvatarUrl is not null)
        {
            try
            {
                await blobService.DeleteAsync(ExtractBlobPath(user.AvatarUrl), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete old avatar blob for user {UserId}. Proceeding with upload.", userId);
            }
        }

        var url = await blobService.UploadAsync(stream, contentType, $"avatars/{userId}", ct);
        user.AvatarUrl = url;
        userRepository.Update(user);
        await unitOfWork.CommitAsync(ct);
        return user;
    }

    public async Task DeleteAvatarAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        if (user.AvatarUrl is null)
            return;

        await blobService.DeleteAsync(ExtractBlobPath(user.AvatarUrl), ct);
        user.AvatarUrl = null;
        userRepository.Update(user);
        await unitOfWork.CommitAsync(ct);
    }

    private static string ExtractBlobPath(string url)
    {
        var path = new Uri(url).AbsolutePath;
        // AbsolutePath = "/{container}/{blobPath}" → strip leading "/{container}/"
        var idx = path.IndexOf('/', 1);
        return idx >= 0 ? path[(idx + 1)..] : path.TrimStart('/');
    }

    public Task<IReadOnlyList<UserSavedPreset>> GetPresetsAsync(Guid userId, CancellationToken ct = default)
        => presetRepository.GetByUserIdAsync(userId, ct);

    public async Task<UserSavedPreset> CreatePresetAsync(Guid userId, string name, string stylesJson, CancellationToken ct = default)
    {
        if (await presetRepository.ExistsByNameAsync(userId, name, ct: ct))
            throw new ConflictException("DUPLICATE_PRESET_NAME",
                "A preset with this name already exists.");

        var preset = new UserSavedPreset
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            StylesJson = stylesJson
        };

        await presetRepository.AddAsync(preset, ct);
        await unitOfWork.CommitAsync(ct);
        return preset;
    }

    public async Task<UserSavedPreset> UpdatePresetAsync(Guid userId, Guid presetId, string? name, string? stylesJson, CancellationToken ct = default)
    {
        var preset = await presetRepository.GetByIdAsync(presetId, ct)
                     ?? throw new NotFoundException("Preset not found.");

        if (preset.UserId != userId)
            throw new ForbiddenException();

        if (name is not null && name != preset.Name)
        {
            if (await presetRepository.ExistsByNameAsync(userId, name, excludePresetId: presetId, ct: ct))
                throw new ConflictException("DUPLICATE_PRESET_NAME",
                    "A preset with this name already exists.");
            preset.Name = name;
        }

        if (stylesJson is not null)
            preset.StylesJson = stylesJson;

        presetRepository.Update(preset);
        await unitOfWork.CommitAsync(ct);
        return preset;
    }

    public async Task DeletePresetAsync(Guid userId, Guid presetId, CancellationToken ct = default)
    {
        var preset = await presetRepository.GetByIdAsync(presetId, ct)
                     ?? throw new NotFoundException("Preset not found.");

        if (preset.UserId != userId)
            throw new ForbiddenException();

        presetRepository.Remove(preset);
        await unitOfWork.CommitAsync(ct);
    }
}
