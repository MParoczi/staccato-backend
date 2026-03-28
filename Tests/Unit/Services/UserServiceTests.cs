using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository>             _userRepo     = new();
    private readonly Mock<IUserSavedPresetRepository>  _presetRepo   = new();
    private readonly Mock<IInstrumentRepository>       _instrumentRepo = new();
    private readonly Mock<IAzureBlobService>           _blobService  = new();
    private readonly Mock<IUnitOfWork>                 _uow          = new();
    private readonly Mock<ILogger<UserService>>        _logger       = new();

    public UserServiceTests()
    {
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _blobService
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.blob.core.windows.net/container/avatars/new-id");
        _blobService
            .Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private UserService CreateService() =>
        new(_userRepo.Object, _presetRepo.Object, _instrumentRepo.Object,
            _blobService.Object, _uow.Object, _logger.Object);

    private static User MakeUser(Guid? id = null, DateTime? scheduledDeletionAt = null, string? avatarUrl = null) =>
        new()
        {
            Id                  = id ?? Guid.NewGuid(),
            Email               = "test@example.com",
            FirstName           = "Test",
            LastName            = "User",
            Language            = Language.English,
            ScheduledDeletionAt = scheduledDeletionAt,
            AvatarUrl           = avatarUrl
        };

    // ── ScheduleDeletion ──────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleDeletion_WhenAlreadyScheduled_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(userId, scheduledDeletionAt: DateTime.UtcNow.AddDays(15));
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService().ScheduleDeletionAsync(userId));

        Assert.Equal("ACCOUNT_DELETION_ALREADY_SCHEDULED", ex.Code);
    }

    [Fact]
    public async Task ScheduleDeletion_WhenNotScheduled_SetsScheduledDeletionAt30DaysOut()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(userId);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        User? captured = null;
        _userRepo.Setup(r => r.Update(It.IsAny<User>()))
            .Callback<User>(u => captured = u);

        await CreateService().ScheduleDeletionAsync(userId);

        Assert.NotNull(captured);
        Assert.NotNull(captured!.ScheduledDeletionAt);
        var diff = captured.ScheduledDeletionAt!.Value - DateTime.UtcNow;
        Assert.True(diff.TotalDays is > 29 and < 31, "Should be ~30 days from now");
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── CancelDeletion ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelDeletion_WhenNotScheduled_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(userId);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => CreateService().CancelDeletionAsync(userId));

        Assert.Equal("ACCOUNT_DELETION_NOT_SCHEDULED", ex.Code);
    }

    [Fact]
    public async Task CancelDeletion_WhenScheduled_ClearsScheduledDeletionAt()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(userId, scheduledDeletionAt: DateTime.UtcNow.AddDays(20));
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        User? captured = null;
        _userRepo.Setup(r => r.Update(It.IsAny<User>()))
            .Callback<User>(u => captured = u);

        await CreateService().CancelDeletionAsync(userId);

        Assert.NotNull(captured);
        Assert.Null(captured!.ScheduledDeletionAt);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UploadAvatar ──────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAvatar_WhenExistingAvatar_DeletesOldBlobFirst()
    {
        var userId = Guid.NewGuid();
        var oldUrl = "https://storage.blob.core.windows.net/container/avatars/old-id";
        var user = MakeUser(userId, avatarUrl: oldUrl);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var callOrder = new List<string>();
        _blobService
            .Setup(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("delete"))
            .Returns(Task.CompletedTask);
        _blobService
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("upload"))
            .ReturnsAsync("https://storage.blob.core.windows.net/container/avatars/new-id");

        await CreateService().UploadAvatarAsync(userId, Stream.Null, "image/jpeg");

        Assert.Equal(new[] { "delete", "upload" }, callOrder);
        _blobService.Verify(b => b.DeleteAsync("avatars/old-id", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── DeleteAvatar ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAvatar_WhenNoAvatar_ReturnsWithoutError()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(userId, avatarUrl: null);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateService().DeleteAvatarAsync(userId);

        _blobService.Verify(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── UpdateProfile ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_WhenInstrumentNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();
        var user = MakeUser(userId);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _instrumentRepo.Setup(r => r.GetByIdAsync(instrumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().UpdateProfileAsync(userId, "First", "Last", Language.English, null, instrumentId));

        Assert.Equal("INSTRUMENT_NOT_FOUND", ex.Code);
    }

    // ── CreatePreset ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePreset_WhenDuplicateName_ThrowsConflictException()
    {
        var userId = Guid.NewGuid();
        _presetRepo.Setup(r => r.ExistsByNameAsync(userId, "My Preset", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService().CreatePresetAsync(userId, "My Preset", "{}"));

        Assert.Equal("DUPLICATE_PRESET_NAME", ex.Code);
    }

    // ── UpdatePreset ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePreset_WhenNotFound_ThrowsNotFoundException()
    {
        var presetId = Guid.NewGuid();
        _presetRepo.Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSavedPreset?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().UpdatePresetAsync(Guid.NewGuid(), presetId, "New Name", null));
    }

    [Fact]
    public async Task UpdatePreset_WhenNotOwner_ThrowsForbiddenException()
    {
        var ownerId  = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        var preset = new UserSavedPreset { Id = presetId, UserId = ownerId, Name = "Preset", StylesJson = "{}" };
        _presetRepo.Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preset);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateService().UpdatePresetAsync(callerId, presetId, "New Name", null));
    }

    [Fact]
    public async Task UpdatePreset_WhenRenameToSameName_Succeeds()
    {
        var userId   = Guid.NewGuid();
        var presetId = Guid.NewGuid();
        const string name = "Unchanged Name";
        var preset = new UserSavedPreset { Id = presetId, UserId = userId, Name = name, StylesJson = "{}" };
        _presetRepo.Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preset);

        var result = await CreateService().UpdatePresetAsync(userId, presetId, name, null);

        Assert.Equal(name, result.Name);
        // ExistsByNameAsync should NOT be called when the name hasn't changed
        _presetRepo.Verify(
            r => r.ExistsByNameAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
