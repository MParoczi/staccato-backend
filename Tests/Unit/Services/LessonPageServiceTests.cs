using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Models;
using Moq;

namespace Tests.Unit.Services;

public class LessonPageServiceTests
{
    private readonly Mock<ILessonRepository> _lessonRepo = new();
    private readonly Mock<ILessonPageRepository> _lessonPageRepo = new();
    private readonly Mock<INotebookRepository> _notebookRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LessonPageService CreateService()
    {
        return new LessonPageService(
            _lessonRepo.Object,
            _lessonPageRepo.Object,
            _notebookRepo.Object,
            _unitOfWork.Object);
    }

    private void SetupLessonOwnership(Guid lessonId, Guid notebookId, Guid userId)
    {
        _lessonRepo.Setup(r => r.GetByIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Lesson { Id = lessonId, NotebookId = notebookId });
        _notebookRepo.Setup(r => r.GetByIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Notebook { Id = notebookId, UserId = userId });
    }

    // ── AddPageAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AddPageAsync_HappyPath_CreatesPageWithCorrectNumber()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        _lessonPageRepo.Setup(r => r.GetMaxPageNumberByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _lessonPageRepo.Setup(r => r.GetPageCountByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var sut = CreateService();
        var (page, isOverSoftLimit) = await sut.AddPageAsync(lessonId, userId);

        Assert.Equal(4, page.PageNumber);
        Assert.Equal(lessonId, page.LessonId);
        Assert.False(isOverSoftLimit);
        _lessonPageRepo.Verify(r => r.AddAsync(It.IsAny<LessonPage>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddPageAsync_Under10Pages_ReturnsSoftLimitFalse()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        _lessonPageRepo.Setup(r => r.GetMaxPageNumberByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);
        _lessonPageRepo.Setup(r => r.GetPageCountByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(9);

        var sut = CreateService();
        var (_, isOverSoftLimit) = await sut.AddPageAsync(lessonId, userId);

        Assert.False(isOverSoftLimit);
    }

    [Fact]
    public async Task AddPageAsync_Exactly10Pages_ReturnsSoftLimitTrue()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        _lessonPageRepo.Setup(r => r.GetMaxPageNumberByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);
        _lessonPageRepo.Setup(r => r.GetPageCountByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var sut = CreateService();
        var (page, isOverSoftLimit) = await sut.AddPageAsync(lessonId, userId);

        Assert.Equal(11, page.PageNumber);
        Assert.True(isOverSoftLimit);
    }

    [Fact]
    public async Task AddPageAsync_Over10Pages_ReturnsSoftLimitTrue()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        _lessonPageRepo.Setup(r => r.GetMaxPageNumberByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);
        _lessonPageRepo.Setup(r => r.GetPageCountByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        var sut = CreateService();
        var (_, isOverSoftLimit) = await sut.AddPageAsync(lessonId, userId);

        Assert.True(isOverSoftLimit);
    }

    [Fact]
    public async Task AddPageAsync_LessonNotFound_ThrowsNotFoundException()
    {
        _lessonRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.AddPageAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task AddPageAsync_WrongUser_ThrowsForbiddenException()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.AddPageAsync(lessonId, Guid.NewGuid()));
    }

    // ── GetByLessonIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetByLessonIdAsync_ReturnsOrderedPages()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        var pages = new List<LessonPage>
        {
            new() { Id = Guid.NewGuid(), LessonId = lessonId, PageNumber = 1, ModuleCount = 2 },
            new() { Id = Guid.NewGuid(), LessonId = lessonId, PageNumber = 2, ModuleCount = 0 }
        };
        _lessonPageRepo.Setup(r => r.GetByLessonIdOrderedAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pages);

        var sut = CreateService();
        var result = await sut.GetByLessonIdAsync(lessonId, userId);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].PageNumber);
        Assert.Equal(2, result[1].PageNumber);
    }

    [Fact]
    public async Task GetByLessonIdAsync_LessonNotFound_ThrowsNotFoundException()
    {
        _lessonRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lesson?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.GetByLessonIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── DeletePageAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeletePageAsync_HappyPath_RemovesAndCommits()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        var page = new LessonPage { Id = pageId, LessonId = lessonId, PageNumber = 2 };
        _lessonPageRepo.Setup(r => r.GetByIdAsync(pageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        _lessonPageRepo.Setup(r => r.GetPageCountByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var sut = CreateService();
        await sut.DeletePageAsync(lessonId, pageId, userId);

        _lessonPageRepo.Verify(r => r.Remove(page), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePageAsync_LastPage_ThrowsBadRequestException()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        var page = new LessonPage { Id = pageId, LessonId = lessonId, PageNumber = 1 };
        _lessonPageRepo.Setup(r => r.GetByIdAsync(pageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        _lessonPageRepo.Setup(r => r.GetPageCountByLessonIdAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateService();
        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => sut.DeletePageAsync(lessonId, pageId, userId));

        Assert.Equal("LAST_PAGE_DELETION", ex.Code);
    }

    [Fact]
    public async Task DeletePageAsync_PageBelongsToDifferentLesson_ThrowsNotFoundException()
    {
        var lessonId = Guid.NewGuid();
        var otherLessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pageId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        var page = new LessonPage { Id = pageId, LessonId = otherLessonId, PageNumber = 1 };
        _lessonPageRepo.Setup(r => r.GetByIdAsync(pageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.DeletePageAsync(lessonId, pageId, userId));
    }

    [Fact]
    public async Task DeletePageAsync_PageNotFound_ThrowsNotFoundException()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, userId);

        _lessonPageRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LessonPage?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.DeletePageAsync(lessonId, Guid.NewGuid(), userId));
    }

    [Fact]
    public async Task DeletePageAsync_WrongUser_ThrowsForbiddenException()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        SetupLessonOwnership(lessonId, notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.DeletePageAsync(lessonId, Guid.NewGuid(), Guid.NewGuid()));
    }
}
