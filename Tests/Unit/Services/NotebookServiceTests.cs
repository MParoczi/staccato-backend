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
}
