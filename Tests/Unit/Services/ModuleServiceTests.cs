using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Moq;

namespace Tests.Unit.Services;

public class ModuleServiceTests
{
    private readonly Mock<IModuleRepository> _moduleRepo = new();
    private readonly Mock<ILessonPageRepository> _lessonPageRepo = new();
    private readonly Mock<ILessonRepository> _lessonRepo = new();
    private readonly Mock<INotebookRepository> _notebookRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private ModuleService CreateService()
    {
        return new ModuleService(
            _moduleRepo.Object,
            _lessonPageRepo.Object,
            _lessonRepo.Object,
            _notebookRepo.Object,
            _unitOfWork.Object);
    }

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _pageId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();
    private readonly Guid _notebookId = Guid.NewGuid();

    private void SetupOwnership()
    {
        _lessonPageRepo.Setup(r => r.GetByIdAsync(_pageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LessonPage { Id = _pageId, LessonId = _lessonId });

        _lessonRepo.Setup(r => r.GetByIdAsync(_lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lesson { Id = _lessonId, NotebookId = _notebookId });

        _notebookRepo.Setup(r => r.GetByIdAsync(_notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Notebook { Id = _notebookId, UserId = _userId, PageSize = PageSize.A4 });
    }

    private void SetupNoOverlap()
    {
        _moduleRepo.Setup(r => r.CheckOverlapAsync(
                _pageId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SetupNoTitleModule()
    {
        _moduleRepo.Setup(r => r.HasTitleModuleInLessonAsync(
                _lessonId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    // ── CreateModuleAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateModuleAsync_HappyPath_ReturnsModuleWithGeneratedId()
    {
        SetupOwnership();
        SetupNoOverlap();

        var sut = CreateService();
        // Theory min size: 8x5, A4 grid: 42x59
        var result = await sut.CreateModuleAsync(
            _pageId, ModuleType.Theory,
            2, 5, 18, 10, 0, "[]", _userId);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_pageId, result.LessonPageId);
        Assert.Equal(ModuleType.Theory, result.ModuleType);
        Assert.Equal(2, result.GridX);
        Assert.Equal(5, result.GridY);
        Assert.Equal(18, result.GridWidth);
        Assert.Equal(10, result.GridHeight);
        Assert.Equal(0, result.ZIndex);
        Assert.Equal("[]", result.ContentJson);

        _moduleRepo.Verify(r => r.AddAsync(It.IsAny<Module>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateModuleAsync_ModuleTooSmall_ThrowsValidationException()
    {
        SetupOwnership();
        SetupNoOverlap();

        var sut = CreateService();
        // Theory min size: 8x5 — trying 6x3
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.CreateModuleAsync(_pageId, ModuleType.Theory, 0, 0, 6, 3, 0, "[]", _userId));

        Assert.Equal("MODULE_TOO_SMALL", ex.Code);
    }

    [Fact]
    public async Task CreateModuleAsync_OutOfBounds_ThrowsValidationException()
    {
        SetupOwnership();
        SetupNoOverlap();

        var sut = CreateService();
        // A4 grid: 42x59. Position 40 + width 5 = 45 > 42
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.CreateModuleAsync(_pageId, ModuleType.Theory, 40, 0, 8, 5, 0, "[]", _userId));

        Assert.Equal("MODULE_OUT_OF_BOUNDS", ex.Code);
    }

    [Fact]
    public async Task CreateModuleAsync_Overlap_ThrowsValidationException()
    {
        SetupOwnership();
        _moduleRepo.Setup(r => r.CheckOverlapAsync(
                _pageId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateService();
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.CreateModuleAsync(_pageId, ModuleType.Theory, 0, 0, 10, 10, 0, "[]", _userId));

        Assert.Equal("MODULE_OVERLAP", ex.Code);
    }

    [Fact]
    public async Task CreateModuleAsync_BreadcrumbWithContent_ThrowsValidationException()
    {
        SetupOwnership();
        SetupNoOverlap();

        var sut = CreateService();
        // Breadcrumb must have empty content — but FR-015 enforces empty on POST for all types,
        // so non-empty content on POST fails first
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.CreateModuleAsync(
                _pageId, ModuleType.Breadcrumb, 0, 0, 20, 3, 0,
                "[{\"type\":\"Text\",\"spans\":[]}]", _userId));

        Assert.Equal("BREADCRUMB_CONTENT_NOT_EMPTY", ex.Code);
    }

    [Fact]
    public async Task CreateModuleAsync_DuplicateTitleModule_ThrowsConflictException()
    {
        SetupOwnership();
        SetupNoOverlap();
        _moduleRepo.Setup(r => r.HasTitleModuleInLessonAsync(
                _lessonId, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateService();
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => sut.CreateModuleAsync(_pageId, ModuleType.Title, 0, 0, 20, 4, 0, "[]", _userId));

        Assert.Equal("DUPLICATE_TITLE_MODULE", ex.Code);
    }

    [Fact]
    public async Task CreateModuleAsync_ValidBoundaryPlacement_Succeeds()
    {
        SetupOwnership();
        SetupNoOverlap();

        var sut = CreateService();
        // A4 grid: 42x59. gridX + gridWidth == 42 is valid (exact boundary)
        // Theory min: 8x5. Use 34 + 8 = 42
        var result = await sut.CreateModuleAsync(
            _pageId, ModuleType.Theory, 34, 0, 8, 5, 0, "[]", _userId);

        Assert.Equal(34, result.GridX);
        Assert.Equal(8, result.GridWidth);
    }

    [Fact]
    public async Task CreateModuleAsync_AdjacentNonOverlapping_Succeeds()
    {
        SetupOwnership();
        SetupNoOverlap();

        var sut = CreateService();
        // Two adjacent modules: first at (0,0,10,10), second at (10,0,10,10)
        // The overlap check is mocked to return false (no overlap)
        var result = await sut.CreateModuleAsync(
            _pageId, ModuleType.Theory, 10, 0, 10, 10, 0, "[]", _userId);

        Assert.Equal(10, result.GridX);
    }

    [Fact]
    public async Task CreateModuleAsync_PageNotFound_ThrowsNotFoundException()
    {
        _lessonPageRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonPage?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.CreateModuleAsync(Guid.NewGuid(), ModuleType.Theory, 0, 0, 10, 10, 0, "[]", _userId));
    }

    [Fact]
    public async Task CreateModuleAsync_OtherUsersPage_ThrowsForbiddenException()
    {
        SetupOwnership();

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.CreateModuleAsync(_pageId, ModuleType.Theory, 0, 0, 10, 10, 0, "[]", Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateModuleAsync_NonEmptyContent_ThrowsValidationException()
    {
        SetupOwnership();

        var sut = CreateService();
        // FR-015: POST must have empty content
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.CreateModuleAsync(
                _pageId, ModuleType.Theory, 0, 0, 10, 10, 0,
                "[{\"type\":\"Text\",\"spans\":[]}]", _userId));

        Assert.Equal("BREADCRUMB_CONTENT_NOT_EMPTY", ex.Code);
    }

    [Fact]
    public async Task CreateModuleAsync_TitleModule_HappyPath()
    {
        SetupOwnership();
        SetupNoOverlap();
        SetupNoTitleModule();

        var sut = CreateService();
        // Title min size: 20x4
        var result = await sut.CreateModuleAsync(
            _pageId, ModuleType.Title, 0, 0, 20, 4, 0, "[]", _userId);

        Assert.Equal(ModuleType.Title, result.ModuleType);
        _moduleRepo.Verify(r => r.HasTitleModuleInLessonAsync(_lessonId, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── UpdateModuleAsync ────────────────────────────────────────────────

    private readonly Guid _moduleId = Guid.NewGuid();

    private void SetupModuleOwnership(ModuleType moduleType = ModuleType.Theory)
    {
        _moduleRepo.Setup(r => r.GetByIdAsync(_moduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Module
            {
                Id = _moduleId,
                LessonPageId = _pageId,
                ModuleType = moduleType,
                GridX = 0,
                GridY = 0,
                GridWidth = 18,
                GridHeight = 10,
                ZIndex = 0,
                ContentJson = "[]"
            });
        SetupOwnership();
        SetupNoOverlap();
    }

    [Fact]
    public async Task UpdateModuleAsync_HappyPath_UpdatesContentAndPosition()
    {
        SetupModuleOwnership();

        var sut = CreateService();
        var contentJson = "[{\"type\":\"SectionHeading\",\"spans\":[{\"text\":\"Title\",\"bold\":false}]},{\"type\":\"Text\",\"spans\":[{\"text\":\"Body\",\"bold\":false}]}]";
        var result = await sut.UpdateModuleAsync(
            _moduleId, ModuleType.Theory,
            5, 10, 18, 12, 1, contentJson, _userId);

        Assert.Equal(5, result.GridX);
        Assert.Equal(10, result.GridY);
        Assert.Equal(12, result.GridHeight);
        Assert.Equal(1, result.ZIndex);
        Assert.Equal(contentJson, result.ContentJson);
        _moduleRepo.Verify(r => r.Update(It.IsAny<Module>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateModuleAsync_InvalidBuildingBlock_ThrowsValidationException()
    {
        SetupModuleOwnership();

        var sut = CreateService();
        // ChordProgression is NOT allowed in Theory modules
        var contentJson = "[{\"type\":\"ChordProgression\",\"chords\":[]}]";
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateModuleAsync(_moduleId, ModuleType.Theory, 0, 0, 18, 10, 0, contentJson, _userId));

        Assert.Equal("INVALID_BUILDING_BLOCK", ex.Code);
    }

    [Fact]
    public async Task UpdateModuleAsync_ModuleTypeImmutable_ThrowsBadRequestException()
    {
        SetupModuleOwnership(ModuleType.Theory);

        var sut = CreateService();
        // Stored as Theory, trying to change to Practice
        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => sut.UpdateModuleAsync(_moduleId, ModuleType.Practice, 0, 0, 18, 10, 0, "[]", _userId));

        Assert.Equal("MODULE_TYPE_IMMUTABLE", ex.Code);
    }

    [Fact]
    public async Task UpdateModuleAsync_BreadcrumbWithContent_ThrowsValidationException()
    {
        SetupModuleOwnership(ModuleType.Breadcrumb);

        var sut = CreateService();
        var contentJson = "[{\"type\":\"Text\",\"spans\":[]}]";
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateModuleAsync(_moduleId, ModuleType.Breadcrumb, 0, 0, 20, 3, 0, contentJson, _userId));

        Assert.Equal("BREADCRUMB_CONTENT_NOT_EMPTY", ex.Code);
    }

    [Fact]
    public async Task UpdateModuleAsync_OverlapExcludesSelf_Succeeds()
    {
        SetupModuleOwnership();
        // Overlap check should exclude the module being updated
        _moduleRepo.Setup(r => r.CheckOverlapAsync(
                _pageId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                _moduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateService();
        var result = await sut.UpdateModuleAsync(
            _moduleId, ModuleType.Theory, 0, 0, 18, 10, 0, "[]", _userId);

        Assert.NotNull(result);
        _moduleRepo.Verify(r => r.CheckOverlapAsync(
            _pageId, 0, 0, 18, 10, _moduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateModuleAsync_OverlapWithOther_ThrowsValidationException()
    {
        SetupModuleOwnership();
        _moduleRepo.Setup(r => r.CheckOverlapAsync(
                _pageId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                _moduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateService();
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateModuleAsync(_moduleId, ModuleType.Theory, 5, 5, 18, 10, 0, "[]", _userId));

        Assert.Equal("MODULE_OVERLAP", ex.Code);
    }

    [Fact]
    public async Task UpdateModuleAsync_ContentOnlyStillRunsGridValidation()
    {
        SetupModuleOwnership();

        var sut = CreateService();
        // Same position but with content — grid validation should still run (FR-018)
        var contentJson = "[{\"type\":\"Text\",\"spans\":[{\"text\":\"Hello\",\"bold\":false}]}]";
        await sut.UpdateModuleAsync(
            _moduleId, ModuleType.Theory, 0, 0, 18, 10, 0, contentJson, _userId);

        // Verify grid validation was invoked
        _moduleRepo.Verify(r => r.CheckOverlapAsync(
            _pageId, 0, 0, 18, 10, _moduleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateModuleAsync_ModuleNotFound_ThrowsNotFoundException()
    {
        _moduleRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Module?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.UpdateModuleAsync(Guid.NewGuid(), ModuleType.Theory, 0, 0, 10, 10, 0, "[]", _userId));
    }

    [Fact]
    public async Task UpdateModuleAsync_OtherUsersModule_ThrowsForbiddenException()
    {
        SetupModuleOwnership();

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.UpdateModuleAsync(_moduleId, ModuleType.Theory, 0, 0, 18, 10, 0, "[]", Guid.NewGuid()));
    }

    // ── UpdateModuleLayoutAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateModuleLayoutAsync_HappyPath_UpdatesPositionLeavesContent()
    {
        SetupModuleOwnership();

        var sut = CreateService();
        var result = await sut.UpdateModuleLayoutAsync(
            _moduleId, 5, 5, 18, 10, 2, _userId);

        Assert.Equal(5, result.GridX);
        Assert.Equal(5, result.GridY);
        Assert.Equal(2, result.ZIndex);
        Assert.Equal("[]", result.ContentJson); // Content unchanged
        Assert.Equal(ModuleType.Theory, result.ModuleType); // Type unchanged
        _moduleRepo.Verify(r => r.Update(It.IsAny<Module>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateModuleLayoutAsync_TooSmall_ThrowsValidationException()
    {
        SetupModuleOwnership();

        var sut = CreateService();
        // Theory min: 8x5, resizing to 6x3
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateModuleLayoutAsync(_moduleId, 0, 0, 6, 3, 0, _userId));

        Assert.Equal("MODULE_TOO_SMALL", ex.Code);
    }

    [Fact]
    public async Task UpdateModuleLayoutAsync_OutOfBounds_ThrowsValidationException()
    {
        SetupModuleOwnership();

        var sut = CreateService();
        // A4: 42x59. gridX(40) + gridWidth(8) = 48 > 42
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateModuleLayoutAsync(_moduleId, 40, 0, 8, 5, 0, _userId));

        Assert.Equal("MODULE_OUT_OF_BOUNDS", ex.Code);
    }

    [Fact]
    public async Task UpdateModuleLayoutAsync_Overlap_ThrowsValidationException()
    {
        SetupModuleOwnership();
        _moduleRepo.Setup(r => r.CheckOverlapAsync(
                _pageId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                _moduleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateService();
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.UpdateModuleLayoutAsync(_moduleId, 5, 5, 18, 10, 0, _userId));

        Assert.Equal("MODULE_OVERLAP", ex.Code);
    }
}
