using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Models;
using Moq;

namespace Tests.Unit.Services;

public class LessonServiceTests
{
    private readonly Mock<ILessonRepository> _lessonRepo = new();
    private readonly Mock<ILessonPageRepository> _lessonPageRepo = new();
    private readonly Mock<INotebookRepository> _notebookRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LessonService CreateService()
    {
        return new LessonService(
            _lessonRepo.Object,
            _lessonPageRepo.Object,
            _notebookRepo.Object,
            _unitOfWork.Object);
    }

    private void SetupNotebook(Guid notebookId, Guid userId)
    {
        _notebookRepo.Setup(r => r.GetByIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Notebook { Id = notebookId, UserId = userId });
    }

    // ── CreateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_HappyPath_CreatesLessonAndFirstPage()
    {
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupNotebook(notebookId, userId);

        var sut = CreateService();
        var (lesson, pages) = await sut.CreateAsync(notebookId, userId, "Guitar Basics");

        Assert.Equal(notebookId, lesson.NotebookId);
        Assert.Equal("Guitar Basics", lesson.Title);
        Assert.Single(pages);
        Assert.Equal(1, pages[0].PageNumber);
        Assert.Equal(lesson.Id, pages[0].LessonId);

        _lessonRepo.Verify(r => r.AddAsync(It.IsAny<Lesson>(), It.IsAny<CancellationToken>()), Times.Once);
        _lessonPageRepo.Verify(r => r.AddAsync(It.IsAny<LessonPage>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        _notebookRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notebook?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), "Title"));
    }

    [Fact]
    public async Task CreateAsync_WrongUser_ThrowsForbiddenException()
    {
        var notebookId = Guid.NewGuid();
        SetupNotebook(notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.CreateAsync(notebookId, Guid.NewGuid(), "Title"));
    }

    // ── GetByNotebookIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetByNotebookIdAsync_ReturnsOrderedSummaries()
    {
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupNotebook(notebookId, userId);

        var summaries = new List<LessonSummary>
        {
            new() { Id = Guid.NewGuid(), Title = "A", PageCount = 1 },
            new() { Id = Guid.NewGuid(), Title = "B", PageCount = 2 }
        };
        _lessonRepo.Setup(r => r.GetSummariesByNotebookIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);

        var sut = CreateService();
        var result = await sut.GetByNotebookIdAsync(notebookId, userId);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByNotebookIdAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        _notebookRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notebook?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.GetByNotebookIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByNotebookIdAsync_WrongUser_ThrowsForbiddenException()
    {
        var notebookId = Guid.NewGuid();
        SetupNotebook(notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetByNotebookIdAsync(notebookId, Guid.NewGuid()));
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_HappyPath_ReturnsLessonWithPages()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var lesson = new Lesson { Id = lessonId, NotebookId = notebookId, Title = "Test" };
        var pages = new List<LessonPage>
        {
            new() { Id = Guid.NewGuid(), LessonId = lessonId, PageNumber = 1, ModuleCount = 0 }
        };

        _lessonRepo.Setup(r => r.GetWithPagesAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((lesson, pages));
        SetupNotebook(notebookId, userId);

        var sut = CreateService();
        var (resultLesson, resultPages) = await sut.GetByIdAsync(lessonId, userId);

        Assert.Equal(lessonId, resultLesson.Id);
        Assert.Single(resultPages);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        _lessonRepo.Setup(r => r.GetWithPagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Lesson, IReadOnlyList<LessonPage>)?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_ThrowsForbiddenException()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();

        var lesson = new Lesson { Id = lessonId, NotebookId = notebookId };
        _lessonRepo.Setup(r => r.GetWithPagesAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((lesson, new List<LessonPage>()));

        SetupNotebook(notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetByIdAsync(lessonId, Guid.NewGuid()));
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_HappyPath_UpdatesTitleAndUpdatedAt()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var originalTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var lesson = new Lesson
        {
            Id = lessonId, NotebookId = notebookId, Title = "Old",
            CreatedAt = originalTime, UpdatedAt = originalTime
        };
        var pages = new List<LessonPage>
        {
            new() { Id = Guid.NewGuid(), LessonId = lessonId, PageNumber = 1 }
        };

        _lessonRepo.Setup(r => r.GetWithPagesAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((lesson, pages));
        SetupNotebook(notebookId, userId);

        var sut = CreateService();
        var (updated, _) = await sut.UpdateAsync(lessonId, userId, "New Title");

        Assert.Equal("New Title", updated.Title);
        Assert.True(updated.UpdatedAt > originalTime);
        _lessonRepo.Verify(r => r.Update(It.IsAny<Lesson>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        _lessonRepo.Setup(r => r.GetWithPagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Lesson, IReadOnlyList<LessonPage>)?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), "Title"));
    }

    [Fact]
    public async Task UpdateAsync_WrongUser_ThrowsForbiddenException()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();

        var lesson = new Lesson { Id = lessonId, NotebookId = notebookId };
        _lessonRepo.Setup(r => r.GetWithPagesAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((lesson, new List<LessonPage>()));

        SetupNotebook(notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.UpdateAsync(lessonId, Guid.NewGuid(), "Title"));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_HappyPath_RemovesAndCommits()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var lesson = new Lesson { Id = lessonId, NotebookId = notebookId };
        _lessonRepo.Setup(r => r.GetWithPagesAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((lesson, new List<LessonPage>()));
        SetupNotebook(notebookId, userId);

        var sut = CreateService();
        await sut.DeleteAsync(lessonId, userId);

        _lessonRepo.Verify(r => r.Remove(lesson), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        _lessonRepo.Setup(r => r.GetWithPagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Lesson, IReadOnlyList<LessonPage>)?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_ThrowsForbiddenException()
    {
        var lessonId = Guid.NewGuid();
        var notebookId = Guid.NewGuid();

        var lesson = new Lesson { Id = lessonId, NotebookId = notebookId };
        _lessonRepo.Setup(r => r.GetWithPagesAsync(lessonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((lesson, new List<LessonPage>()));

        SetupNotebook(notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.DeleteAsync(lessonId, Guid.NewGuid()));
    }

    // ── GetNotebookIndexAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetNotebookIndexAsync_ThreeLessons_ReturnsCorrectStartPageNumbers()
    {
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupNotebook(notebookId, userId);

        var summaries = new List<LessonSummary>
        {
            new() { Id = Guid.NewGuid(), Title = "A", CreatedAt = DateTime.UtcNow, PageCount = 3 },
            new() { Id = Guid.NewGuid(), Title = "B", CreatedAt = DateTime.UtcNow, PageCount = 2 },
            new() { Id = Guid.NewGuid(), Title = "C", CreatedAt = DateTime.UtcNow, PageCount = 1 }
        };
        _lessonRepo.Setup(r => r.GetSummariesByNotebookIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);

        var sut = CreateService();
        var result = await sut.GetNotebookIndexAsync(notebookId, userId);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].StartPageNumber);   // 2 + 0
        Assert.Equal(5, result[1].StartPageNumber);   // 2 + 3
        Assert.Equal(7, result[2].StartPageNumber);   // 2 + 3 + 2
    }

    [Fact]
    public async Task GetNotebookIndexAsync_EmptyNotebook_ReturnsEmptyList()
    {
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupNotebook(notebookId, userId);

        _lessonRepo.Setup(r => r.GetSummariesByNotebookIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LessonSummary>());

        var sut = CreateService();
        var result = await sut.GetNotebookIndexAsync(notebookId, userId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNotebookIndexAsync_SingleLesson_ReturnsStartPageNumber2()
    {
        var notebookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        SetupNotebook(notebookId, userId);

        var summaries = new List<LessonSummary>
        {
            new() { Id = Guid.NewGuid(), Title = "Solo", CreatedAt = DateTime.UtcNow, PageCount = 5 }
        };
        _lessonRepo.Setup(r => r.GetSummariesByNotebookIdAsync(notebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);

        var sut = CreateService();
        var result = await sut.GetNotebookIndexAsync(notebookId, userId);

        Assert.Single(result);
        Assert.Equal(2, result[0].StartPageNumber);
    }

    [Fact]
    public async Task GetNotebookIndexAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        _notebookRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notebook?)null);

        var sut = CreateService();
        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.GetNotebookIndexAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetNotebookIndexAsync_WrongUser_ThrowsForbiddenException()
    {
        var notebookId = Guid.NewGuid();
        SetupNotebook(notebookId, Guid.NewGuid());

        var sut = CreateService();
        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetNotebookIndexAsync(notebookId, Guid.NewGuid()));
    }
}
