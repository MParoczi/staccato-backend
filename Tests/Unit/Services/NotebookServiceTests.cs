using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Moq;

namespace Tests.Unit.Services;

public class NotebookServiceTests
{
    private readonly Mock<INotebookRepository>             _notebookRepo    = new();
    private readonly Mock<INotebookModuleStyleRepository>  _styleRepo       = new();
    private readonly Mock<ISystemStylePresetRepository>    _systemPresetRepo = new();
    private readonly Mock<IUserSavedPresetRepository>      _userPresetRepo  = new();
    private readonly Mock<IInstrumentRepository>           _instrumentRepo  = new();
    private readonly Mock<IPdfExportRepository>            _pdfExportRepo   = new();
    private readonly Mock<IUnitOfWork>                     _unitOfWork      = new();

    private NotebookService CreateService() => new(
        _notebookRepo.Object,
        _styleRepo.Object,
        _systemPresetRepo.Object,
        _userPresetRepo.Object,
        _instrumentRepo.Object,
        _pdfExportRepo.Object,
        _unitOfWork.Object);

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string BuildPresetStylesJson()
    {
        var moduleTypes = Enum.GetValues<ModuleType>();
        var entries = moduleTypes.Select(mt => $@"
            {{
                ""moduleType"": ""{mt}"",
                ""backgroundColor"": ""#ffffff"",
                ""borderColor"": ""#000000"",
                ""borderStyle"": ""None"",
                ""borderWidth"": 0,
                ""borderRadius"": 0,
                ""headerBgColor"": ""#eeeeee"",
                ""headerTextColor"": ""#333333"",
                ""bodyTextColor"": ""#000000"",
                ""fontFamily"": ""Default""
            }}");
        return $"[{string.Join(",", entries)}]";
    }

    private static IReadOnlyList<NotebookModuleStyle> BuildStyles(Guid notebookId)
    {
        return Enum.GetValues<ModuleType>()
            .Select(mt => new NotebookModuleStyle
            {
                Id         = Guid.NewGuid(),
                NotebookId = notebookId,
                ModuleType = mt,
                StylesJson = @"{""backgroundColor"":""#fff"",""borderColor"":""#000"",""borderStyle"":""None"",""borderWidth"":0,""borderRadius"":0,""headerBgColor"":""#eee"",""headerTextColor"":""#333"",""bodyTextColor"":""#000"",""fontFamily"":""Default""}"
            })
            .ToList();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithNullStyles_AppliesColorfulPreset()
    {
        var userId       = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();
        var notebookId   = Guid.NewGuid();

        var instrument = new Instrument
        {
            Id          = instrumentId,
            Key         = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar",
            StringCount = 6
        };

        var preset = new SystemStylePreset
        {
            Id           = Guid.NewGuid(),
            Name         = "Colorful",
            DisplayOrder = 2,
            IsDefault    = true,
            StylesJson   = BuildPresetStylesJson()
        };

        var notebook = new Notebook
        {
            Id           = notebookId,
            UserId       = userId,
            Title        = "My Notebook",
            InstrumentId = instrumentId,
            PageSize     = PageSize.A4,
            CoverColor   = "#ff0000",
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        var styles = BuildStyles(notebookId);

        _instrumentRepo
            .Setup(r => r.GetByIdAsync(instrumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        _systemPresetRepo
            .Setup(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(preset);

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, styles));

        var svc = CreateService();
        var (resultNotebook, resultStyles) = await svc.CreateAsync(
            userId, "My Notebook", instrumentId, PageSize.A4, "#ff0000", null);

        Assert.Equal(userId, resultNotebook.UserId);
        Assert.Equal(12, resultStyles.Count);

        _instrumentRepo.Verify(r => r.GetByIdAsync(instrumentId, It.IsAny<CancellationToken>()), Times.Once);
        _systemPresetRepo.Verify(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Once);
        _styleRepo.Verify(r => r.AddAsync(It.IsAny<NotebookModuleStyle>(), It.IsAny<CancellationToken>()), Times.Exactly(12));
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithExplicitStyles_UsesProvidedStyles()
    {
        var userId       = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();
        var notebookId   = Guid.NewGuid();

        var instrument = new Instrument
        {
            Id          = instrumentId,
            Key         = InstrumentKey.Guitar6String,
            DisplayName = "6-String Guitar",
            StringCount = 6
        };

        var notebook = new Notebook
        {
            Id           = notebookId,
            UserId       = userId,
            Title        = "Custom",
            InstrumentId = instrumentId,
            PageSize     = PageSize.A5,
            CoverColor   = "#0000ff",
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        };

        var providedStyles = BuildStyles(notebookId);
        var returnedStyles = BuildStyles(notebookId);

        _instrumentRepo
            .Setup(r => r.GetByIdAsync(instrumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, returnedStyles));

        var svc = CreateService();
        var (resultNotebook, resultStyles) = await svc.CreateAsync(
            userId, "Custom", instrumentId, PageSize.A5, "#0000ff", providedStyles);

        Assert.Equal(12, resultStyles.Count);

        // Default preset must NOT be fetched when explicit styles are provided
        _systemPresetRepo.Verify(r => r.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
        _styleRepo.Verify(r => r.AddAsync(It.IsAny<NotebookModuleStyle>(), It.IsAny<CancellationToken>()), Times.Exactly(12));
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownInstrumentId_ThrowsInstrumentNotFoundException()
    {
        var userId       = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();

        _instrumentRepo
            .Setup(r => r.GetByIdAsync(instrumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Instrument?)null);

        var svc = CreateService();

        await Assert.ThrowsAsync<InstrumentNotFoundException>(() =>
            svc.CreateAsync(userId, "Test", instrumentId, PageSize.A4, "#ffffff", null));

        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException()
    {
        var notebookId   = Guid.NewGuid();
        var ownerId      = Guid.NewGuid();
        var requesterId  = Guid.NewGuid();

        var notebook = new Notebook
        {
            Id     = notebookId,
            UserId = ownerId  // belongs to a different user
        };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        var svc = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.GetByIdAsync(notebookId, requesterId));
    }

    [Fact]
    public async Task GetByIdAsync_NotebookDoesNotExist_ThrowsNotFoundException()
    {
        var notebookId = Guid.NewGuid();

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((Notebook, IReadOnlyList<NotebookModuleStyle>)?));

        var svc = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.GetByIdAsync(notebookId, Guid.NewGuid()));
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ChangesOnlyTitleAndCoverColor()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();

        var notebook = new Notebook
        {
            Id         = notebookId,
            UserId     = userId,
            Title      = "Original Title",
            CoverColor = "#000000",
            UpdatedAt  = DateTime.UtcNow.AddDays(-1)
        };

        var styles = BuildStyles(notebookId);

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)styles));

        var svc = CreateService();
        var (result, _) = await svc.UpdateAsync(notebookId, userId, "New Title", "#ffffff");

        Assert.Equal("New Title", result.Title);
        Assert.Equal("#ffffff", result.CoverColor);

        _notebookRepo.Verify(r => r.Update(It.IsAny<Notebook>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        var notebookId = Guid.NewGuid();

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((Notebook, IReadOnlyList<NotebookModuleStyle>)?));

        var svc = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(notebookId, Guid.NewGuid(), "New Title", "#ffffff"));
    }

    [Fact]
    public async Task UpdateAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException()
    {
        var notebookId  = Guid.NewGuid();
        var ownerId     = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var notebook = new Notebook { Id = notebookId, UserId = ownerId };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        var svc = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.UpdateAsync(notebookId, requesterId, "New Title", "#ffffff"));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesNotebook()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();

        var notebook = new Notebook { Id = notebookId, UserId = userId };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        _pdfExportRepo
            .Setup(r => r.HasActiveExportForNotebookAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var svc = CreateService();
        await svc.DeleteAsync(notebookId, userId);

        _notebookRepo.Verify(r => r.Remove(notebook), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        var notebookId = Guid.NewGuid();

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((Notebook, IReadOnlyList<NotebookModuleStyle>)?));

        var svc = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.DeleteAsync(notebookId, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException()
    {
        var notebookId  = Guid.NewGuid();
        var ownerId     = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var notebook = new Notebook { Id = notebookId, UserId = ownerId };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        var svc = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.DeleteAsync(notebookId, requesterId));
    }

    [Fact]
    public async Task DeleteAsync_ActiveExportExists_ThrowsConflictException()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();

        var notebook = new Notebook { Id = notebookId, UserId = userId };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        _pdfExportRepo
            .Setup(r => r.HasActiveExportForNotebookAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var svc = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.DeleteAsync(notebookId, userId));

        _notebookRepo.Verify(r => r.Remove(It.IsAny<Notebook>()), Times.Never);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetStylesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetStylesAsync_ReturnsAllTwelveStyles()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var notebook   = new Notebook { Id = notebookId, UserId = userId };
        var styles     = BuildStyles(notebookId);

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)styles));

        var svc    = CreateService();
        var result = await svc.GetStylesAsync(notebookId, userId);

        Assert.Equal(12, result.Count);
    }

    [Fact]
    public async Task GetStylesAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        var notebookId = Guid.NewGuid();

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((Notebook, IReadOnlyList<NotebookModuleStyle>)?));

        var svc = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.GetStylesAsync(notebookId, Guid.NewGuid()));
    }

    [Fact]
    public async Task GetStylesAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException()
    {
        var notebookId  = Guid.NewGuid();
        var ownerId     = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var notebook    = new Notebook { Id = notebookId, UserId = ownerId };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        var svc = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.GetStylesAsync(notebookId, requesterId));
    }

    // ── BulkUpdateStylesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task BulkUpdateStylesAsync_ReplacesAllTwelveStyles()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var notebook   = new Notebook { Id = notebookId, UserId = userId };
        var existing   = BuildStyles(notebookId);
        var incoming   = BuildStyles(notebookId);  // fresh set (same shape, different object)

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        _styleRepo
            .SetupSequence(r => r.GetByNotebookIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(existing);

        var svc = CreateService();
        var result = await svc.BulkUpdateStylesAsync(notebookId, userId, incoming);

        Assert.Equal(12, result.Count);
        _styleRepo.Verify(r => r.Update(It.IsAny<NotebookModuleStyle>()), Times.Exactly(12));
        _notebookRepo.Verify(r => r.Update(It.IsAny<Notebook>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateStylesAsync_InvalidStyleCount_ThrowsBadRequestException()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();

        // Only 6 styles — invalid
        var insufficient = BuildStyles(notebookId).Take(6).ToList();

        var svc = CreateService();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.BulkUpdateStylesAsync(notebookId, userId, insufficient));

        _notebookRepo.Verify(r => r.GetWithStylesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkUpdateStylesAsync_NotebookBelongsToOtherUser_ThrowsForbiddenException()
    {
        var notebookId  = Guid.NewGuid();
        var ownerId     = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var notebook    = new Notebook { Id = notebookId, UserId = ownerId };
        var incoming    = BuildStyles(notebookId);

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        var svc = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.BulkUpdateStylesAsync(notebookId, requesterId, incoming));
    }

    [Fact]
    public async Task BulkUpdateStylesAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        var notebookId = Guid.NewGuid();
        var incoming   = BuildStyles(notebookId);

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default((Notebook, IReadOnlyList<NotebookModuleStyle>)?));

        var svc = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.BulkUpdateStylesAsync(notebookId, Guid.NewGuid(), incoming));
    }

    // ── ApplyPresetAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyPresetAsync_SystemPreset_UpdatesAllTwelveStyles()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var presetId   = Guid.NewGuid();

        var notebook = new Notebook { Id = notebookId, UserId = userId };
        var existing = BuildStyles(notebookId);

        var preset = new SystemStylePreset
        {
            Id         = presetId,
            Name       = "Classic",
            IsDefault  = false,
            StylesJson = BuildPresetStylesJson()
        };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        _systemPresetRepo
            .Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preset);

        _styleRepo
            .SetupSequence(r => r.GetByNotebookIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(existing);

        var svc    = CreateService();
        var result = await svc.ApplyPresetAsync(notebookId, userId, presetId);

        Assert.Equal(12, result.Count);
        _styleRepo.Verify(r => r.Update(It.IsAny<NotebookModuleStyle>()), Times.Exactly(12));
        _notebookRepo.Verify(r => r.Update(It.IsAny<Notebook>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyPresetAsync_UserPreset_OwnershipMismatch_ThrowsForbidden()
    {
        var userId      = Guid.NewGuid();
        var notebookId  = Guid.NewGuid();
        var presetId    = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var notebook = new Notebook { Id = notebookId, UserId = userId };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        _systemPresetRepo
            .Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemStylePreset?)null);

        _userPresetRepo
            .Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSavedPreset
            {
                Id         = presetId,
                UserId     = otherUserId,  // different user
                Name       = "Custom",
                StylesJson = "[]"
            });

        var svc = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            svc.ApplyPresetAsync(notebookId, userId, presetId));
    }

    [Fact]
    public async Task ApplyPresetAsync_PresetNotFound_ThrowsNotFoundException()
    {
        var userId     = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var presetId   = Guid.NewGuid();

        var notebook = new Notebook { Id = notebookId, UserId = userId };

        _notebookRepo
            .Setup(r => r.GetWithStylesAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notebook, (IReadOnlyList<NotebookModuleStyle>)new List<NotebookModuleStyle>()));

        _systemPresetRepo
            .Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemStylePreset?)null);

        _userPresetRepo
            .Setup(r => r.GetByIdAsync(presetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSavedPreset?)null);

        var svc = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.ApplyPresetAsync(notebookId, userId, presetId));
    }
}
