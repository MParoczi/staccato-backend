using Domain.Exceptions;
using Domain.Interfaces;
using Domain.Interfaces.Repositories;
using Domain.Services;
using DomainModels.Enums;
using DomainModels.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Tests.Unit.Services;

public class PdfExportServiceTests
{
    private readonly Mock<IPdfExportRepository> _exportRepo = new();
    private readonly Mock<INotebookRepository> _notebookRepo = new();
    private readonly Mock<ILessonRepository> _lessonRepo = new();
    private readonly Mock<IPdfExportQueue> _queue = new();
    private readonly Mock<IAzureBlobService> _blobService = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PdfExportService CreateService()
    {
        return new PdfExportService(
            _exportRepo.Object, _notebookRepo.Object, _lessonRepo.Object,
            _queue.Object, _blobService.Object, _uow.Object,
            new NullLogger<PdfExportService>());
    }

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid NotebookId = Guid.NewGuid();

    private static Notebook MakeNotebook(Guid? userId = null)
    {
        return new Notebook
        {
            Id = NotebookId,
            UserId = userId ?? UserId,
            Title = "My Notebook",
            PageSize = PageSize.A4,
            CoverColor = "#FF0000",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static PdfExport MakeExport(
        ExportStatus status = ExportStatus.Pending,
        Guid? userId = null,
        string? blobRef = null,
        DateTime? completedAt = null)
    {
        return new PdfExport
        {
            Id = Guid.NewGuid(),
            NotebookId = NotebookId,
            UserId = userId ?? UserId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CompletedAt = completedAt,
            BlobReference = blobRef
        };
    }

    // ── QueueExportAsync ───────────────────────────────────────────────

    [Fact]
    public async Task QueueExportAsync_ValidRequest_CreatesExportAndEnqueues()
    {
        var notebook = MakeNotebook();
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notebook);
        _exportRepo
            .Setup(r => r.HasActiveExportForNotebookAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().QueueExportAsync(UserId, NotebookId, null);

        Assert.Equal(NotebookId, result.NotebookId);
        Assert.Equal(UserId, result.UserId);
        Assert.Equal(ExportStatus.Pending, result.Status);
        _exportRepo.Verify(r => r.AddAsync(It.IsAny<PdfExport>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _queue.Verify(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueExportAsync_WithLessonIds_DeduplicatesAndStores()
    {
        var lessonId = Guid.NewGuid();
        var notebook = MakeNotebook();
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notebook);
        _exportRepo
            .Setup(r => r.HasActiveExportForNotebookAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _lessonRepo
            .Setup(r => r.GetByNotebookIdOrderedByCreatedAtAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson> { new() { Id = lessonId, NotebookId = NotebookId } });

        PdfExport? captured = null;
        _exportRepo
            .Setup(r => r.AddAsync(It.IsAny<PdfExport>(), It.IsAny<CancellationToken>()))
            .Callback<PdfExport, CancellationToken>((e, _) => captured = e);

        await CreateService().QueueExportAsync(UserId, NotebookId, [lessonId, lessonId, lessonId]);

        Assert.NotNull(captured);
        Assert.Single(captured!.LessonIds!);
        Assert.Equal(lessonId, captured.LessonIds![0]);
    }

    [Fact]
    public async Task QueueExportAsync_WithValidLessonIds_Succeeds()
    {
        var lesson1 = Guid.NewGuid();
        var lesson2 = Guid.NewGuid();
        var notebook = MakeNotebook();
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notebook);
        _exportRepo
            .Setup(r => r.HasActiveExportForNotebookAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _lessonRepo
            .Setup(r => r.GetByNotebookIdOrderedByCreatedAtAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson>
            {
                new() { Id = lesson1, NotebookId = NotebookId },
                new() { Id = lesson2, NotebookId = NotebookId }
            });

        var result = await CreateService().QueueExportAsync(UserId, NotebookId, [lesson1, lesson2]);

        Assert.Equal(ExportStatus.Pending, result.Status);
        Assert.Equal(2, result.LessonIds!.Count);
        _queue.Verify(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueExportAsync_InvalidLessonIds_ThrowsBadRequestException()
    {
        var validLesson = Guid.NewGuid();
        var invalidLesson = Guid.NewGuid();
        var notebook = MakeNotebook();
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notebook);
        _exportRepo
            .Setup(r => r.HasActiveExportForNotebookAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _lessonRepo
            .Setup(r => r.GetByNotebookIdOrderedByCreatedAtAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Lesson>
            {
                new() { Id = validLesson, NotebookId = NotebookId }
            });

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => CreateService().QueueExportAsync(UserId, NotebookId, [validLesson, invalidLesson]));
        Assert.Equal("INVALID_LESSON_IDS", ex.Code);
    }

    [Fact]
    public async Task QueueExportAsync_NotebookNotFound_ThrowsNotFoundException()
    {
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notebook?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().QueueExportAsync(UserId, NotebookId, null));
    }

    [Fact]
    public async Task QueueExportAsync_NotOwner_ThrowsForbiddenException()
    {
        var notebook = MakeNotebook(Guid.NewGuid()); // different owner
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notebook);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateService().QueueExportAsync(UserId, NotebookId, null));
    }

    [Fact]
    public async Task QueueExportAsync_ActiveExportExists_ThrowsConflictException()
    {
        var notebook = MakeNotebook();
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notebook);
        _exportRepo
            .Setup(r => r.HasActiveExportForNotebookAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => CreateService().QueueExportAsync(UserId, NotebookId, null));
        Assert.Equal("ACTIVE_EXPORT_EXISTS", ex.Code);
    }

    // ── GetExportByIdAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetExportByIdAsync_ValidOwner_ReturnsExport()
    {
        var export = MakeExport();
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        var result = await CreateService().GetExportByIdAsync(export.Id, UserId);

        Assert.Equal(export.Id, result.Id);
    }

    [Fact]
    public async Task GetExportByIdAsync_NotFound_ThrowsNotFoundException()
    {
        var exportId = Guid.NewGuid();
        _exportRepo
            .Setup(r => r.GetByIdAsync(exportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PdfExport?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().GetExportByIdAsync(exportId, UserId));
    }

    [Fact]
    public async Task GetExportByIdAsync_WrongUser_ThrowsForbiddenException()
    {
        var export = MakeExport(userId: Guid.NewGuid());
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateService().GetExportByIdAsync(export.Id, UserId));
    }

    // ── DownloadExportAsync ────────────────────────────────────────────

    [Fact]
    public async Task DownloadExportAsync_ReadyExport_ReturnsStreamAndFileName()
    {
        var export = MakeExport(ExportStatus.Ready, blobRef: "exports/blob.pdf",
            completedAt: DateTime.UtcNow);
        var notebook = MakeNotebook();
        var stream = new MemoryStream([0x25, 0x50, 0x44, 0x46]); // %PDF

        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);
        _blobService
            .Setup(b => b.GetStreamAsync("exports/blob.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);
        _notebookRepo
            .Setup(r => r.GetByIdAsync(NotebookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notebook);

        var (content, fileName, contentType) =
            await CreateService().DownloadExportAsync(export.Id, UserId);

        Assert.Same(stream, content);
        Assert.Equal("My Notebook.pdf", fileName);
        Assert.Equal("application/pdf", contentType);
    }

    [Fact]
    public async Task DownloadExportAsync_NotReady_ThrowsNotFoundException()
    {
        var export = MakeExport(ExportStatus.Processing);
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().DownloadExportAsync(export.Id, UserId));
        Assert.Equal("EXPORT_NOT_READY", ex.Code);
    }

    [Fact]
    public async Task DownloadExportAsync_Expired_ThrowsNotFoundException()
    {
        var export = MakeExport(ExportStatus.Ready, blobRef: "blob.pdf",
            completedAt: DateTime.UtcNow.AddHours(-25));
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().DownloadExportAsync(export.Id, UserId));
        Assert.Equal("EXPORT_EXPIRED", ex.Code);
    }

    [Fact]
    public async Task DownloadExportAsync_WrongUser_ThrowsForbiddenException()
    {
        var export = MakeExport(ExportStatus.Ready, userId: Guid.NewGuid(),
            blobRef: "blob.pdf", completedAt: DateTime.UtcNow);
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateService().DownloadExportAsync(export.Id, UserId));
    }

    // ── DeleteExportAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteExportAsync_WithBlob_DeletesBlobAndRecord()
    {
        var export = MakeExport(ExportStatus.Ready, blobRef: "exports/blob.pdf");
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await CreateService().DeleteExportAsync(export.Id, UserId);

        _blobService.Verify(b => b.DeleteAsync("exports/blob.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _exportRepo.Verify(r => r.Remove(export), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteExportAsync_NoBlob_SkipsBlobDeleteAndRemovesRecord()
    {
        var export = MakeExport(ExportStatus.Pending);
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await CreateService().DeleteExportAsync(export.Id, UserId);

        _blobService.Verify(b => b.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _exportRepo.Verify(r => r.Remove(export), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteExportAsync_WrongUser_ThrowsForbiddenException()
    {
        var export = MakeExport(userId: Guid.NewGuid());
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => CreateService().DeleteExportAsync(export.Id, UserId));
    }

    [Fact]
    public async Task DeleteExportAsync_NotFound_ThrowsNotFoundException()
    {
        var exportId = Guid.NewGuid();
        _exportRepo
            .Setup(r => r.GetByIdAsync(exportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PdfExport?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateService().DeleteExportAsync(exportId, UserId));
    }

    // ── MarkAsProcessingAsync ──────────────────────────────────────────

    [Fact]
    public async Task MarkAsProcessingAsync_ExistingExport_SetsStatusAndCommits()
    {
        var export = MakeExport(ExportStatus.Pending);
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await CreateService().MarkAsProcessingAsync(export.Id);

        Assert.Equal(ExportStatus.Processing, export.Status);
        _exportRepo.Verify(r => r.Update(export), Times.Once);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsProcessingAsync_NotFound_NoOp()
    {
        _exportRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PdfExport?)null);

        await CreateService().MarkAsProcessingAsync(Guid.NewGuid());

        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── MarkAsReadyAsync ───────────────────────────────────────────────

    [Fact]
    public async Task MarkAsReadyAsync_ExistingExport_SetsStatusBlobAndCompletedAt()
    {
        var export = MakeExport(ExportStatus.Processing);
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await CreateService().MarkAsReadyAsync(export.Id, "exports/blob.pdf");

        Assert.Equal(ExportStatus.Ready, export.Status);
        Assert.Equal("exports/blob.pdf", export.BlobReference);
        Assert.NotNull(export.CompletedAt);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── MarkAsFailedAsync ──────────────────────────────────────────────

    [Fact]
    public async Task MarkAsFailedAsync_ExistingExport_SetsFailedAndCompletedAt()
    {
        var export = MakeExport(ExportStatus.Processing);
        _exportRepo
            .Setup(r => r.GetByIdAsync(export.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(export);

        await CreateService().MarkAsFailedAsync(export.Id);

        Assert.Equal(ExportStatus.Failed, export.Status);
        Assert.NotNull(export.CompletedAt);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── ResetStaleProcessingExportsAsync ────────────────────────────────

    [Fact]
    public async Task ResetStaleProcessingExportsAsync_StaleExports_ResetsAndReturnsIds()
    {
        var e1 = MakeExport(ExportStatus.Processing);
        var e2 = MakeExport(ExportStatus.Processing);
        _exportRepo
            .Setup(r => r.GetByStatusAsync(ExportStatus.Processing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PdfExport> { e1, e2 });

        var result = await CreateService().ResetStaleProcessingExportsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(ExportStatus.Pending, e1.Status);
        Assert.Equal(ExportStatus.Pending, e2.Status);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetStaleProcessingExportsAsync_NoStale_DoesNotCommit()
    {
        _exportRepo
            .Setup(r => r.GetByStatusAsync(ExportStatus.Processing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PdfExport>());

        var result = await CreateService().ResetStaleProcessingExportsAsync();

        Assert.Empty(result);
        _uow.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── GetExportsByUserAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetExportsByUserAsync_ReturnsUserExports()
    {
        var exports = new List<PdfExport> { MakeExport(), MakeExport() };
        _exportRepo
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exports);

        var result = await CreateService().GetExportsByUserAsync(UserId);

        Assert.Equal(2, result.Count);
    }
}
